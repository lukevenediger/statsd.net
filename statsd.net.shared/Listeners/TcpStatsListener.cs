using statsd.net.shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Listeners
{
  public class TcpStatsListener : IListener
  {
    private ITargetBlock<string> _target;
    private CancellationToken _token;
    private ISystemMetricsService _systemMetrics;

    public TcpStatsListener(int port, ISystemMetricsService systemMetrics)
    {
      _systemMetrics = systemMetrics;
    }

    public void LinkTo(ITargetBlock<string> target, CancellationToken token)
    {
      _target = target;
      _token = token;
    }
  }
}
