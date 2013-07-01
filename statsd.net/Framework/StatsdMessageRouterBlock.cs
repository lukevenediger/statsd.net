using statsd.net.shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.Framework
{
  internal class StatsdMessageRouterBlock : ITargetBlock<StatsdMessage>
  {
    private ITargetBlock<StatsdMessage> _gauges;
    private ITargetBlock<StatsdMessage> _counters;
    private List<ITargetBlock<StatsdMessage>> _timings;

    public StatsdMessageRouterBlock()
    {
      _timings = new List<ITargetBlock<StatsdMessage>>();
    }

    public void AddTarget(MessageType message, ITargetBlock<StatsdMessage> target)
    {
      switch (message)
      {
        case MessageType.Counter: _counters = target; break;
        case MessageType.Gauge: _gauges = target; break;
        case MessageType.Timing: _timings.Add(target); break;
      }
    }
    
    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, StatsdMessage messageValue, ISourceBlock<StatsdMessage> source, bool consumeToAccept)
    {
      switch (messageValue.MessageType)
      {
        case MessageType.Counter: _counters.Post(messageValue as Counter); break;
        case MessageType.Gauge: _gauges.Post(messageValue as Gauge); break;
        case MessageType.Timing: 
          if (_timings.Count == 1) 
          {
            _timings[0].Post(messageValue as Timing);
          }
          else 
          {
            _timings.ForEach(p => p.Post(messageValue as Timing));
          }
          break;
        case MessageType.Invalid:
          break;
        default:
          throw new ArgumentOutOfRangeException("StatsdMessage.MessageType", messageValue.MessageType.ToString()); 
      }
      return DataflowMessageStatus.Accepted;
    }

    public void Complete()
    {
      _gauges.Complete();
      _counters.Complete();
      _timings.ForEach(p => p.Complete());
    }

    public Task Completion
    {
      get { throw new NotImplementedException(); }
    }

    public void Fault(Exception exception)
    {
      throw new NotImplementedException();
    }
  }
}
