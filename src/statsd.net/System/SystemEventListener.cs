using statsd.net.Listeners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.System
{
  public sealed class SystemEventListener : IListener
  {
    private ITargetBlock<string> _target;

    public void LinkTo(ITargetBlock<string> target, CancellationToken token)
    {
      _target = target;
    }

    public void Send(string message)
    {
      _target.Post(message);
    }
  }
}
