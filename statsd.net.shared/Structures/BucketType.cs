using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Structures
{
  public enum BucketType
  {
    Count,
    Timing,
    Gauge,
    Set,
    Percentile
  }
}
