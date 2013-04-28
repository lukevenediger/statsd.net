using StatsdClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net_Tests.Infrastructure
{
  internal class InAppListenerOutputChannel : IOutputChannel
  {
    private InAppListener _listener;

    public InAppListenerOutputChannel(InAppListener listener)
    {
      _listener = listener;
    }

    public void Send(string line)
    {
      _listener.Send(line);
    }
  }
}
