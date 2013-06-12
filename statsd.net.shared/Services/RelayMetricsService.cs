using statsd.net.shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Services
{
  public class RelayMetricsService : ISystemMetricsService
  {
    private string _prefix;
    private ITargetBlock<StatsdMessage> _target;

    public RelayMetricsService(string serviceName, string prefix = null)
    {
      _prefix = serviceName + "." + (String.IsNullOrEmpty(prefix) ? String.Empty : (prefix + "."));
    }

    public void SetTarget(ITargetBlock<StatsdMessage> target)
    {
      _target = target;
    }

    public void LogCount(string name, int count = 1)
    {
      _target.Post(new Counter(name, count));
    }
  }
}
