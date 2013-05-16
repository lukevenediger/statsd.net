using statsd.net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.Backends
{
  public class ConsoleBackend : IBackend
  {
    private bool _isActive;
    private Task _completionTask;

    public ConsoleBackend()
    {
      _isActive = true;
      _completionTask = new Task(() => { _isActive = false; });
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, GraphiteLine messageValue, ISourceBlock<GraphiteLine> source, bool consumeToAccept)
    {
      Console.WriteLine(messageValue);
      return DataflowMessageStatus.Accepted;
    }

    public void Complete()
    {
      Console.WriteLine("Done");
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
      get { return 0; }
    }
  }
}
