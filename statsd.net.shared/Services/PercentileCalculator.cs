using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Services
{
  public static class PercentileCalculator
  {
    public static bool TryCompute(List<double> data, int percentile, out double percentileValue)
    {
      percentileValue = 0;
      if (percentile < 0 || percentile > 100)
      {
        throw new ArgumentException("Percentile value must be between 0 and 1.0");
      }

      if (data.Count < 3)  
      {
        return false;
      }
      data.Sort();

      // take the first
      if (percentile == 0)
      {
        percentileValue = data[0];
        return true;
      }

      // take the last
      if (percentile == 100)
      {
        percentileValue = data[data.Count - 1];
        return true;
      }
      var n = (int)Math.Round((data.Count * (percentile / 100.0)) + 0.5, 0);
      percentileValue = data[n - 1];
      return true;
    }
  }
}