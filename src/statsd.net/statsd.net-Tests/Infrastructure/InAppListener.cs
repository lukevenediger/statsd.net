using statsd.net.Listeners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net_Tests.Infrastructure
{
  public class InAppListener : IListener
  {
    private ITargetBlock<string> _target;

    public void LinkTo(ITargetBlock<string> target)
    {
      _target = target;
    }

    public void Send(string message)
    {
      _target.Post(message);
    }
  }
}
