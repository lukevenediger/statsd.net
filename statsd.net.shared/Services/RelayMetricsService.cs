using statsd.net.shared.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Services
{
  public class RelayMetricsService : ISystemMetricsService
  {
    private string _prefix;
    private ITargetBlock<StatsdMessage> _target;
    private ConcurrentDictionary<string, int> _counts;
    private IIntervalService _intervalService;

    public RelayMetricsService(string serviceName, 
      CancellationToken cancellationToken,
      string prefix = null)
    {
      _prefix = serviceName + "." + (String.IsNullOrEmpty(prefix) ? String.Empty : (prefix + "."));
      _counts = new ConcurrentDictionary<string, int>();
      _intervalService = new IntervalService(60, cancellationToken);
      _intervalService.Elapsed += SendPendingMetrics;
      _intervalService.Start();
    }

    private void SendPendingMetrics(object sender, IntervalFiredEventArgs e)
    {
      var metrics = _counts.ToArray().Select(x => new Counter(_prefix + x.Key, x.Value)).ToList();
      _counts.Clear();
      metrics.ForEach(p => _target.Post(p));
    }

    public void SetTarget(ITargetBlock<StatsdMessage> target)
    {
      _target = target;
    }

    public void LogCount(string name, int count = 1)
    {
      _counts.AddOrUpdate(name, count, (key, current) => current + count);
    }
  }
}
