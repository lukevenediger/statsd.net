using statsd.net.Backends;
using statsd.net.Listeners;
using statsd.net.Messages;
using statsd.net.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net.Services;

namespace statsd.net
{
  public class Statsd
  {
    private TransformBlock<string, StatsdMessage> _messageParser;
    private StatsdMessageRouterBlock _router;
    private BroadcastBlock<GraphiteLine> _messageBroadcaster;
    private List<IBackend> _backends;
    private List<IListener> _listeners;
    private CancellationTokenSource _tokenSource;
    private ManualResetEvent _shutdownComplete;

    public WaitHandle ShutdownWaitHandle
    {
      get
      {
        return _shutdownComplete;
      }
    }
    
    public Statsd()
    {
      LoggingBootstrap.Configure();
      _tokenSource = new CancellationTokenSource();
      _shutdownComplete = new ManualResetEvent(false);

      SuperCheapIOC.Add( new SystemMetricsService() as ISystemMetricsService );
      
      /**
       * The flow is:
       *  Listeners ->
       *    Message Parser ->
       *      router ->
       *        Aggregator ->
       *          Broadcaster ->
       *            Backends
       */ 
      
      // Initialise the core blocks
      _router = new StatsdMessageRouterBlock();
      _messageParser = MessageParserBlockFactory.CreateMessageParserBlock(_tokenSource.Token, 
        SuperCheapIOC.Resolve<ISystemMetricsService>() );
      _messageParser.LinkTo(_router);
      _messageParser.Completion.ContinueWith(_ =>
        {
          _messageBroadcaster.Complete();
        });
      _messageBroadcaster = new BroadcastBlock<GraphiteLine>(GraphiteLine.Clone);
      _messageBroadcaster.Completion.ContinueWith(_ =>
        {
          _backends.ForEach(q => q.Complete());
        });

      // Add the broadcaster to the IOC container
      SuperCheapIOC.Add<ITargetBlock<GraphiteLine>>(_messageBroadcaster);

      _backends = new List<IBackend>();
      _listeners = new List<IListener>();
    }

    public Statsd(dynamic config) 
      : this()
    {
      var systemMetrics = SuperCheapIOC.Resolve<ISystemMetricsService>();

      // Load listeners
      if (config.listeners.udp.enabled)
      {
        AddListener(new UdpStatsListener((int)config.listeners.udp.port, systemMetrics));
      }

      // Load backends
      if (config.backends.console.enabled)
      {
        AddBackend(new ConsoleBackend());
      }
      if (config.backends.graphite.enabled)
      {
        AddBackend(new GraphiteBackend(config.backends.graphite.host, (int)config.backends.graphite.port, systemMetrics));
      }
      if (config.backends.sqlserver.enabled)
      {
        AddBackend(new SqlServerBackend(config.backends.sqlserver.connectionString, config.general.name, systemMetrics));
      }

      // Load Aggregators
      int flushPeriod = (int)config.calc.flushIntervalSeconds;
      AddAggregator(MessageType.Counter,
        TimedCounterAggregatorBlockFactory.CreateBlock(_messageBroadcaster, config.calc.countersNamespace, new IntervalService(flushPeriod)));
      AddAggregator(MessageType.Gauge,
        TimedGaugeAggregatorBlockFactory.CreateBlock(_messageBroadcaster, config.calc.gaugesNamespace, new IntervalService(flushPeriod)));
      AddAggregator(MessageType.Set,
        TimedSetAggregatorBlockFactory.CreateBlock(_messageBroadcaster, config.calc.setsNamespace, new IntervalService(flushPeriod)));
      AddAggregator(MessageType.Timing,
        TimedLatencyAggregatorBlockFactory.CreateBlock(_messageBroadcaster, config.calc.timersNamespace, new IntervalService(flushPeriod)));
      // Load Latency Percentile Aggregators
      foreach (var percentile in (IDictionary<string, object>)config.calc.percentiles)
      {
        dynamic thePercentile = percentile.Value;
        AddAggregator(MessageType.Timing,
          TimedLatencyPercentileAggregatorBlockFactory.CreateBlock(_messageBroadcaster, config.calc.timersNamespace + "." + percentile.Key,
            new IntervalService((int)thePercentile.flushIntervalSeconds),
            (int)thePercentile.percentile ));
      }
    }

    public void AddListener(IListener listener)
    {
      _listeners.Add(listener);
      listener.LinkTo(_messageParser, _tokenSource.Token); 
    }

    public void AddAggregator(MessageType targetType, ActionBlock<StatsdMessage> aggregator)
    {
      _router.AddTarget(targetType, aggregator);
    }

    public void AddBackend(IBackend backend)
    {
      _backends.Add(backend);
      _messageBroadcaster.LinkTo(backend);
      backend.Completion.ContinueWith(p =>
        {
          if (_backends.All(q => !q.IsActive))
          {
            _shutdownComplete.Set();
          }
        });
    }

    public void Stop()
    {
      _tokenSource.Cancel();
      _shutdownComplete.WaitOne();
    }
  }
}
