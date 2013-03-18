using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsdClient
{
  public static class StatsdClientExtensions
  {
    public static void LogTiming(this IStatsdClient client, string name, TimeSpan duration)
    {
      client.LogTiming(name, (int)duration.TotalMilliseconds);
    }

    public static TimingToken LogTiming(this IStatsdClient client, string name)
    {
      return new TimingToken(client, name);
    }
  }
}
