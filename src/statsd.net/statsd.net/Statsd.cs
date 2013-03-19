using statsd.net.Backends;
using statsd.net.Listeners;
using statsd.net.Messages;
using statsd.net.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net
{
  public class Statsd
  {
    private TransformBlock<string, StatsdMessage> _messageParser;
    private StatsdMessageRouterBlock _router;
    private BroadcastBlock<GraphiteLine> _messageBroadcaster;
    private List<ITargetBlock<GraphiteLine>> _backends;
    private List<IListener> _listeners;
    private CancellationTokenSource _tokenSource;
    
    public Statsd()
    {
      _tokenSource = new CancellationTokenSource();

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
      _messageParser = MessageParserBlockFactory.CreateMessageParserBlock(_tokenSource.Token);
      _messageParser.LinkTo(_router);
      _messageBroadcaster = new BroadcastBlock<GraphiteLine>(GraphiteLine.Clone);

      _backends = new List<ITargetBlock<GraphiteLine>>();
      _listeners = new List<IListener>();
    }

    public Statsd(dynamic config, CancellationTokenSource tokenSource) 
      : this()
    {
      _tokenSource = tokenSource;
      // Load listeners
      if (config.listeners.udp.enabled)
      {
        AddListener(new UdpStatsListener((int)config.listeners.udp.port));
      }

      // Load backends
      if (config.backends.console.enabled)
      {
        AddBackend(new ConsoleBackend());
      }

      // Load Aggregators
      AddAggregator(MessageType.Counter,
        AggregatorFactory.CreateTimedCountersBlock(config.calc.countersNamespace, new TimeSpan(0, 0, (int)config.calc.flushIntervalSeconds)));
      AddAggregator(MessageType.Gauge,
        AggregatorFactory.CreateTimedGaugesBlock(config.calc.gaugesNamespace, new TimeSpan(0, 0, (int)config.calc.flushIntervalSeconds)));
      foreach (var timer in (IDictionary<string, object>)config.calc.timers)
      {
        dynamic theTimer = timer.Value;
        AddAggregator(MessageType.Timing,
          AggregatorFactory.CreateTimedLatencyBlock(config.calc.timersNamespace + "." + timer.Key, 
            new TimeSpan(0, 0, (int)theTimer.flushIntervalSeconds), 
            (int)theTimer.percentile ));
      }
    }

    public void AddListener(IListener listener)
    {
      _listeners.Add(listener);
      listener.LinkTo(_messageParser, _tokenSource.Token); 
    }

    public void AddAggregator(MessageType targetType, IPropagatorBlock<StatsdMessage, GraphiteLine> aggregator)
    {
      _router.AddTarget(targetType, aggregator);
      aggregator.LinkTo(_messageBroadcaster);
    }

    public void ConfigureCounters(string rootNamespace = "stats_counts", int flushIntervalSeconds = 60)
    {
      // TimedDataBlockFactory
    }

    public void AddBackend(ITargetBlock<GraphiteLine> backend)
    {
      _backends.Add(backend);
      _messageBroadcaster.LinkTo(backend);
    }

    public void Stop()
    {
      _tokenSource.Cancel();
    }
  }
}
