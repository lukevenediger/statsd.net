using statsd.net.Backends;
using statsd.net.shared;
using statsd.net.shared.Backends;
using statsd.net.shared.Messages;
using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net_Tests.Infrastructure
{
  public class InAppBackend : IBackend
  {
    private bool _isActive;
    private Task _completionTask;
    private ActionBlock<GraphiteLine> _collationTarget;
    
    public List<GraphiteLine> Messages { get; private set; }

    public InAppBackend()
    {
      Messages = new List<GraphiteLine>();
      _completionTask = new Task(() => { _isActive = false; });
      _collationTarget = new ActionBlock<GraphiteLine>(p => Messages.Add(p), Utility.OneAtATimeExecution());
      _isActive = true;
    }
    
    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, Bucket messageValue, ISourceBlock<Bucket> source, bool consumeToAccept)
    {
      messageValue.FeedTarget(_collationTarget);
      return DataflowMessageStatus.Accepted;
    }

    public void Complete()
    {
      _completionTask.Start();
    }

    public Task Completion
    {
      get { return _completionTask; }
    }

    public void Fault(Exception exception)
    {
      throw new NotImplementedException();
    }

    public bool IsActive
    {
      get { return _isActive; }
    }

    public int OutputCount
    {
      get { return Messages.Count; }
    }
  }
}
