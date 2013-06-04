using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace statsd.relay
{
  public class Relay
  {
    public WaitHandle ShutdownWaitHandle { get; set; }

    public Relay(dynamic config)
    {
    }

    public void Stop()
    {
    }
  }
}
