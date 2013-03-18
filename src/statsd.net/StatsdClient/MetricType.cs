using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsdClient
{
  public static class MetricType
  {
    public const string COUNT = "c";
    public const string TIMING = "ms";
    public const string GAUGE = "g";
    public const string SET = "s";
  }
}
