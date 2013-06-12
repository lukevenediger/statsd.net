using statsd.net.shared.Listeners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net_Tests.Infrastructure
{
  public class InAppListener : IListener
  {
    private ITargetBlock<string> _target;
    public bool IsListening { get; set; }

    public void LinkTo(ITargetBlock<string> target, CancellationToken cancellationToken)
    {
      _target = target;
      IsListening = true;
    }

    public void Send(string message)
    {
      _target.Post(message);
    }
  }
}
