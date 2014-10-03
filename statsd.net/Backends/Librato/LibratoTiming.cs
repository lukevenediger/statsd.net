using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace statsd.net.Backends.Librato
{
    public class LibratoTiming : LibratoMetric
    {
      public string name { get; set; }
      public int count { get; set; }
      public double sum { get; set; }
      public double sum_squares { get; set; }
      public double min { get; set; }
      public double max { get; set; }

      public LibratoTiming(string name, 
        int count,
        double sum,
        double sumOfSquares,
        double min,
        double max,
        long epoch)
        : base(LibratoMetricType.Timing, epoch)
      {
        this.name = Regex.Replace(name, LibratoBackend.ILLEGAL_NAME_CHARACTERS, "_");
        this.count = count;
        this.sum = sum;
        this.sum_squares = sumOfSquares;
        this.min = min;
        this.max = max;
      }
    }
}
