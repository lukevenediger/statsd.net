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
      public int sum { get; set; }
      public int sum_squares { get; set; }
      public int min { get; set; }
      public int max { get; set; }

      public LibratoTiming(string name, 
        int count,
        int sum,
        int sumOfSquares,
        int min,
        int max,
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
