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
    private BroadcastBlock<GraphiteLine> _messageBroadcaster;
    private List<ITargetBlock<GraphiteLine>> _backends;
    private CancellationTokenSource _tokenSource;
    
    public Statsd()
    {
      _tokenSource = new CancellationTokenSource();
      
      // Initialise the core blocks
      _messageParser = MessageParserBlockFactory.CreateMessageParserBlock(_tokenSource.Token);
      _messageParser.LinkTo(_router);
      _messageBroadcaster = new BroadcastBlock<GraphiteLine>(GraphiteLine.Clone);

      _backends = new List<ITargetBlock<GraphiteLine>>();
    }

    public void AddListener(string type, int port)
    {
    }

    public void AddAggregator(MessageType targetType, IPropagatorBlock<StatsdMessage, GraphiteLine> aggregator)
    {
      _router.AddTarget(targetType, aggregator);
      aggregator.LinkTo(_messageBroadcaster);
    }

    public void AddBackend(ITargetBlock<GraphiteLine> backend)
    {
      _backends.Add(backend);
      _messageBroadcaster.LinkTo(backend);
    }
  }
}
