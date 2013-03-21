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
    private List<IBackend> _backends;
    private List<IListener> _listeners;
    private CancellationTokenSource _tokenSource;
    private ManualResetEvent _shutdownComplete;
    private SystemEventListener _systemEvents;
    
    public Statsd()
    {
      _tokenSource = new CancellationTokenSource();
      _shutdownComplete = new ManualResetEvent(false);
      _systemEvents = new SystemEventListener();

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
      _messageParser = MessageParserBlockFactory.CreateMessageParserBlock(_tokenSource.Token, _systemEvents);
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

      _backends = new List<IBackend>();
      _listeners = new List<IListener>();

      // Add the system events listener
      AddListener(_systemEvents);
    }

    public Statsd(dynamic config) 
      : this()
    {
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
