using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using statsd.net.core.Structures;

namespace statsd.net.core.Backends
{
  public interface IBackend : ITargetBlock<Bucket>
  {
    string Name { get; }
    bool IsActive { get; }
    int OutputCount { get; }
    void Configure(string collectorName, XElement configElement, ISystemMetricsService systemMetrics);
  }
}
