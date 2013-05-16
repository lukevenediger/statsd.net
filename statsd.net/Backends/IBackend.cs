using statsd.net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.Backends
{
  public interface IBackend : ITargetBlock<GraphiteLine>
  {
    bool IsActive { get; }
    int OutputCount { get; }
  }
}
