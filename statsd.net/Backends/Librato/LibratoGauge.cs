using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace statsd.net.Backends.Librato
{
  [DebuggerDisplay("{name} = {value}")]
  public class LibratoGauge : LibratoMetric
  {
    public string name { get; set; }
    public int value { get; set; }

    public LibratoGauge(string name, int value, long epoch)
      : base(LibratoMetricType.Gauge, epoch)
    {
      this.name = Regex.Replace(name, LibratoBackend.ILLEGAL_NAME_CHARACTERS, "_");
      this.value = value;
    }
  }
}
