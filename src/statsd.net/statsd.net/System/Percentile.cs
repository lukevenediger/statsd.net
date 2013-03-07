using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.System
{
  /// <summary>
  /// Methods to calculate the percentiles.
  /// </summary>
  public enum PercentileMethod
  {
    /// <summary>
    /// Using the method recommened my NIST,
    /// http://www.itl.nist.gov/div898/handbook/prc/section2/prc252.htm
    /// </summary>
    Nist = 0,

    /// <summary>
    /// Using the nearest rank, http://en.wikipedia.org/wiki/Percentile#Nearest_Rank
    /// </summary>
    Nearest,

    /// <summary>
    /// Using the same method as Excel does, 
    /// http://www.itl.nist.gov/div898/handbook/prc/section2/prc252.htm
    /// </summary>
    Excel,

    /// <summary>
    /// Use linear interpolation between the two nearest ranks,
    /// http://en.wikipedia.org/wiki/Percentile#Linear_Interpolation_Between_Closest_Ranks
    /// </summary>
    Interpolation
  }

  /// <summary>
  /// Class to calculate percentiles.
  /// </summary>
  public static class Percentile
  {
    public static bool TryCompute(List<int> data, double percentile, PercentileMethod method, out int percentileValue)
    {
      percentileValue = 0;
      if (percentile < 0 || percentile > 100)
      {
        throw new ArgumentException("Percentile value must be between 0 and 100.");
      }

      if (data.Count < 3)  
      {
        return false;
      }
      data.Sort();

      // take the first
      if (percentile == 0.0)
      {
        percentileValue = data[0];
        return true;
      }

      // take the last
      if (percentile == 1.0)
      {
        percentileValue = data[data.Count - 1];
        return true;
      }

      switch (method)
      {
        case PercentileMethod.Nist:
          percentileValue = Nist(data, percentile);
          break;
        case PercentileMethod.Nearest:
          percentileValue = Nearest(data, percentile);
          break;
        case PercentileMethod.Interpolation:
          percentileValue = Interpolation(data, percentile);
          break;
        case PercentileMethod.Excel:
          percentileValue = Excel(data, percentile);
          break;
      }

      return true;
    }

    private static int Nearest(List<int> data, double percentile)
    {
      var n = (int)Math.Round((data.Count * percentile) + 0.5, 0);
      return data[n - 1];
    }

    private static int Excel(List<int> data, double percentile)
    {
      var tmp = 1 + (percentile * (data.Count - 1.0));
      var k = (int)tmp;
      var d = tmp - k;

      return data[k - 1] + (d * (data[k] - data[k - 1]));
    }

    private static int Interpolation(List<int> data, double percentile)
    {
      var k = (int)(data.Count * percentile);
      var pk = (k - 0.5) / data.Count;
      return data[k - 1] + (data.Count * (percentile - pk) * (data[k] - data[k - 1]));
    }

    private static int Nist(List<int> data, double percentile)
    {
      var tmp = percentile * (data.Count + 1.0);
      var k = (int)tmp;
      var d = tmp - k;

      return data[k - 1] + (d * (data[k] - data[k - 1]));
    }
  }
}