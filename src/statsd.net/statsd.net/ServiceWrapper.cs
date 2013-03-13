using statsd.net.Listeners;
using statsd.net.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Topshelf;

namespace statsd.net
{
  public class ServiceWrapper : ServiceControl
  {
    CancellationTokenSource _tokenSource;
    public ServiceWrapper()
    {
      _tokenSource = new CancellationTokenSource();
    }

    public bool Start(HostControl hostControl)
    {
      return true;
    }

    public bool Stop(HostControl hostControl)
    {
      return false;
    }
  }
}
