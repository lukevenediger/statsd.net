using statsd.net.Backends;
using statsd.net.shared.Listeners;
using statsd.net.shared.Messages;
using statsd.net.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net.shared.Services;
using log4net;
using statsd.net.Backends.SqlServer;
using statsd.net.shared.Backends;
using statsd.net.shared;
using statsd.net.shared.Factories;

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
    private static readonly ILog _log = LogManager.GetLogger("statsd.net");

    public WaitHandle ShutdownWaitHandle
    {
      get
      {
        return _shutdownComplete;
      }
    }

    public Statsd ( string serviceName = null )
    {
      LoggingBootstrap.Configure();
      _log.Info( "statsd.net starting." );
      _tokenSource = new CancellationTokenSource();
      _shutdownComplete = new ManualResetEvent( false );

      SuperCheapIOC.Add( _log );
      var systemInfoService = new SystemInfoService();
      SuperCheapIOC.Add( systemInfoService as ISystemInfoService );
      serviceName = serviceName ?? systemInfoService.HostName;
      var systemMetricsService = new SystemMetricsService( "statsdnet", serviceName );
      SuperCheapIOC.Add( systemMetricsService as ISystemMetricsService );

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
      _messageParser = MessageParserBlockFactory.CreateMessageParserBlock( _tokenSource.Token,
        SuperCheapIOC.Resolve<ISystemMetricsService>(),
        _log);
      _messageParser.LinkTo( _router );
      _messageParser.Completion.LogAndContinueWith( _log, "MessageParser", () =>
        {
          _log.Info("MessageParser: Completion signaled. Notifying the MessageBroadcaster.");
          _messageBroadcaster.Complete();
        } );
      _messageBroadcaster = new BroadcastBlock<GraphiteLine>( GraphiteLine.Clone );
      _messageBroadcaster.Completion.LogAndContinueWith( _log, "MessageBroadcaster", () =>
        {
          _log.Info("MessageBroadcaster: Completion signaled. Notifying all backends.");
          _backends.ForEach( q => q.Complete() );
        } );

      // Add the broadcaster to the IOC container
      SuperCheapIOC.Add<BroadcastBlock<GraphiteLine>>( _messageBroadcaster );
      systemMetricsService.SetTarget( _messageBroadcaster );

      _backends = new List<IBackend>();
      _listeners = new List<IListener>();
    }

    public Statsd(dynamic config)
       : this((string)config.general.name)
    {
      _log.Info("statsd.net loading config.");
      var systemMetrics = SuperCheapIOC.Resolve<ISystemMetricsService>();

      // Load backends
      if (config.backends.console.enabled)
      {
        AddBackend(new ConsoleBackend(), "console");
      }
      if (config.backends.graphite.enabled)
      {
        AddBackend(new GraphiteBackend(config.backends.graphite.host, (int)config.backends.graphite.port, systemMetrics), "graphite");
      }
      if (config.backends.sqlserver.enabled)
      {
        AddBackend(new SqlServerBackend(
          config.backends.sqlserver.connectionString, 
          config.general.name, 
          systemMetrics, 
          batchSize : (int)config.backends.sqlserver.writeBatchSize), 
        "sqlserver");
      }

      // Load Aggregators
      var intervalServices = new List<IIntervalService>();
      var intervalService = new IntervalService( ( int )config.calc.flushIntervalSeconds );
      intervalServices.Add(intervalService);
      AddAggregator(MessageType.Counter,
        TimedCounterAggregatorBlockFactory.CreateBlock(_messageBroadcaster, config.calc.countersNamespace, intervalService, _log));
      AddAggregator(MessageType.Gauge,
        TimedGaugeAggregatorBlockFactory.CreateBlock(_messageBroadcaster, config.calc.gaugesNamespace, intervalService, _log));
      AddAggregator(MessageType.Set,
        TimedSetAggregatorBlockFactory.CreateBlock(_messageBroadcaster, config.calc.setsNamespace, intervalService, _log));
      AddAggregator(MessageType.Timing,
        TimedLatencyAggregatorBlockFactory.CreateBlock(_messageBroadcaster, config.calc.timersNamespace, intervalService, _log));

      // Load Latency Percentile Aggregators
      foreach (var percentile in (IDictionary<string, object>)config.calc.percentiles)
      {
        dynamic thePercentile = percentile.Value;
        intervalService = new IntervalService((int)thePercentile.flushIntervalSeconds);
        AddAggregator(MessageType.Timing,
          TimedLatencyPercentileAggregatorBlockFactory.CreateBlock(_messageBroadcaster, config.calc.timersNamespace,
            intervalService,
            (int)thePercentile.percentile,
            percentile.Key,
            _log));
        intervalServices.Add(intervalService);
      }

      // Load listeners - done last and once the rest of the chain is in place
      if (config.listeners.udp.enabled)
      {
        AddListener(new UdpStatsListener((int)config.listeners.udp.port, systemMetrics));
      }
      if (config.listeners.tcp.enabled)
      {
        AddListener(new TcpStatsListener((int)config.listeners.tcp.port, systemMetrics));
      }
      if (config.listeners.http.enabled)
      {
        AddListener(new HttpStatsListener((int)config.listeners.http.port, systemMetrics));
      }

      // Now start the interval service
      intervalServices.ForEach(p => p.Start());
    }

    public void AddListener(IListener listener)
    {
      _log.InfoFormat("Adding listener {0}", listener.GetType().Name);
      _listeners.Add(listener);
      listener.LinkTo(_messageParser, _tokenSource.Token); 
    }

    public void AddAggregator(MessageType targetType, ActionBlock<StatsdMessage> aggregator)
    {
      _router.AddTarget(targetType, aggregator);
    }

    public void AddBackend(IBackend backend, string name = "")
    {
      _log.InfoFormat("Adding backend {0} named '{1}'", backend.GetType().Name, name);
      _backends.Add(backend);
      SuperCheapIOC.Add(backend, name);
      _messageBroadcaster.LinkTo(backend);
      backend.Completion.LogAndContinueWith(_log, name, () =>
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
