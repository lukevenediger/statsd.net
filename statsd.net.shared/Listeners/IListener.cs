using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Listeners
{
  public interface IListener
  {
    void LinkTo(ITargetBlock<string> target, CancellationToken token);
    bool IsListening { get; }
  }
}
