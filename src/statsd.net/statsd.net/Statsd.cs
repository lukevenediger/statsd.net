using statsd.net.Listeners;
using statsd.net.Messages;
using statsd.net.System;
using System;
using System.Collections.Generic;
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
    private BroadcastBlock<GraphiteLine[]> _messageBroadcaster;
    private List<ITargetBlock<GraphiteLine[]>> _backends;
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
      _messageBroadcaster = new BroadcastBlock<GraphiteLine[]>(GraphiteLine.CloneMany);

      _backends = new List<ITargetBlock<GraphiteLine[]>>();
      _listeners = new List<IListener>();
    }

    public void AddListener(IListener listener)
    {
      _listeners.Add(listener);
      listener.LinkTo(_messageParser); 
    }

    public void AddAggregator(MessageType targetType, IPropagatorBlock<StatsdMessage, GraphiteLine[]> aggregator)
    {
      _router.AddTarget(targetType, aggregator);
      aggregator.LinkTo(_messageBroadcaster);
    }

    public void ConfigureCounters(string rootNamespace = "stats_counts", int flushIntervalSeconds = 60)
    {
      // TimedDataBlockFactory
    }

    public void AddBackend(ITargetBlock<GraphiteLine[]> backend)
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
