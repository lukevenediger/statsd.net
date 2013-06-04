using statsd.net.shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Backends
{
  public interface IBackend : ITargetBlock<GraphiteLine>
  {
    bool IsActive { get; }
    int OutputCount { get; }
  }
}
