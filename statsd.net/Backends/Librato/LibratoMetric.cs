using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Backends.Librato
{
  public abstract class LibratoMetric
  {
    public LibratoMetricType MetricType { get; private set; }
    public long Epoch { get; private set; }
    public LibratoMetric(LibratoMetricType type, long epoch)
    {
      MetricType = type;
      Epoch = epoch;
    }
  }
}
