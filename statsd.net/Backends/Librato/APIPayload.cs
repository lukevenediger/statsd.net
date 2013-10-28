using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Backends.Librato
{
  internal class APIPayload
  {
    public string source { get; set; }
    public LibratoMetric[] gauges { get; set; }
    public LibratoMetric[] counters { get; set; }
    public long measure_time { get; set; }
  }
}
