using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net.core;
using statsd.net.core.Backends;
using statsd.net.core.Structures;
using statsd.net.shared.Listeners;
using statsd.net.shared.Messages;
using statsd.net.shared.Structures;

namespace statsd.net.shared.Services
{

  /// <summary>
  /// Keeps track of things like bad lines, failed sends, lines processed etc.
  /// </summary>
  public class SystemMetricsService : ISystemMetricsService
  {
    private string _prefix;
    private ITargetBlock<Bucket> _target;
    private ConcurrentDictionary<string, int> _metrics;
    public bool HideSystemStats { get; set; }

    public SystemMetricsService(string serviceName, string prefix = null, IIntervalService intervalService = null, bool hideSystemStats = false)
    {
      if (intervalService == null)
      {
        intervalService = new IntervalService(10);
      }
      _prefix = serviceName + "." + (String.IsNullOrEmpty(prefix) ? String.Empty : (prefix + "."));
      _metrics = new ConcurrentDictionary<string, int>();
      HideSystemStats = hideSystemStats;
      intervalService.Elapsed += SendMetrics;
      intervalService.Start();
   }

    public void LogCount(string name, int quantity = 1)
    {
      _metrics.AddOrUpdate(name, quantity, (key, input) => { return input + quantity; });
    }

    public void LogGauge(string name, int value)
    {
      _metrics.AddOrUpdate(name, value, (key, input) => { return value; });
    }

    public void SetTarget(ITargetBlock<Bucket> target)
    {
      _target = target;
    }

    private void SendMetrics(object sender, IntervalFiredEventArgs args)
    {
      if (_target == null)
      {
        return;
      }

      // Get a count of metrics waiting to be sent out
      var outputBufferCount = SuperCheapIOC.ResolveAll<IBackend>().Sum(p => p.OutputCount);
      LogGauge("outputBuffer", outputBufferCount);
      LogGauge("up", 1);

      var bucket = new CounterBucket(_metrics.ToArray(), args.Epoch, _prefix);
      _metrics.Clear();
      if ( !HideSystemStats )
      {
        _target.Post( bucket );
      }
    }
  }
}
