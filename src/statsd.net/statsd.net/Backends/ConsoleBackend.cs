using statsd.net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.Backends
{
  class ConsoleBackend : ITargetBlock<GraphiteLine>
  {
    public ConsoleBackend()
    {
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, GraphiteLine messageValue, ISourceBlock<GraphiteLine> source, bool consumeToAccept)
    {
      Console.WriteLine(messageValue.ToString());
      return DataflowMessageStatus.Accepted;
    }

    public void Complete()
    {
      Console.WriteLine("Done");
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
