using System.Xml.Linq;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Backends
{
  public interface IBackend : ITargetBlock<Bucket>
  {
    string Name { get; }
    bool IsActive { get; }
    int OutputCount { get; }
    void Configure(string collectorName, XElement configElement, ISystemMetricsService systemMetrics);
  }
}
