using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net_Tests.Infrastructure
{
  [DebuggerDisplay("{Items.Count} items")]
  public class OutputBufferBlock<T> : ITargetBlock<T>
  {
    public List<T> Items { get; private set; }

    public T this[int index]
    {
      get { return Items[index]; } 
    }

    public OutputBufferBlock()
    {
      Items = new List<T>();
    }
    
    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
    {
      Items.Add(messageValue);
      return DataflowMessageStatus.Accepted;
    }

    public void Complete()
    {
      // NOOP
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
