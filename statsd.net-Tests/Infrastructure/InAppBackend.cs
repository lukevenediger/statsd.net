using statsd.net.Backends;
using statsd.net.shared.Backends;
using statsd.net.shared.Messages;
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
    
    public List<GraphiteLine> Messages { get; private set; }

    public InAppBackend()
    {
      Messages = new List<GraphiteLine>();
      _completionTask = new Task(() => { _isActive = false; });
      _isActive = true;
    }
    
    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, GraphiteLine messageValue, ISourceBlock<GraphiteLine> source, bool consumeToAccept)
    {
      Messages.Add(messageValue);
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
