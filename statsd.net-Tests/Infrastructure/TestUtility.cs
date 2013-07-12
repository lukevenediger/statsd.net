using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net_Tests.Infrastructure
{
  internal class TestUtility
  {
    private static Random _random = new Random();

    public static List<int> Range(int max, bool zeroBased = true)
    {
      var items = new List<int>();
      for (int i = 0; i < max; i++)
      {
        items.Add((zeroBased ? i : i + 1));
      }
      return items;
    }

    public static TimeSpan OneSecondTimeout
    {
      get
      {
        return new TimeSpan(0, 0, 1);
      }
    }

    public static int NextInteger
    {
      get
      {
        return _random.Next();
      }
    }
  }
}
