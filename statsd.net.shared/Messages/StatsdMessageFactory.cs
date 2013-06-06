using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Messages
{
  public static class StatsdMessageFactory
  {
    private static char[] splitter = new char[] { '|' };

    public static bool TryParseMessage(string line, out StatsdMessage message)
    {
      try
      {
        message = ParseMessage(line);
        return true;
      }
      catch (Exception)
      {
        message = null;
        return false;
      }
    }

    public static StatsdMessage ParseMessage(string line)
    {
      string[] nameAndValue = line.Split(':');
      if (nameAndValue[0].Length == 0)
      {
        throw new ArgumentOutOfRangeException("Name cannot be empty.");
      }
      string[] statProperties = nameAndValue[1].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
      switch (statProperties[1])
      {
        case "c":
          if (statProperties.Length == 2)
          {
            // gorets:1|c
            return new Counter(nameAndValue[0], Int32.Parse(statProperties[0]));
          }
          else
          {
            // gorets:1|c|@0.1
            return new Counter(nameAndValue[0], Int32.Parse(statProperties[0]), float.Parse(statProperties[2].Remove(0, 1)));
          }
        case "ms":
          // glork:320|ms
          return new Timing(nameAndValue[0], Int32.Parse(statProperties[0]));
        case "g":
          // gaugor:333|g
          return new Gauge(nameAndValue[0], Int32.Parse(statProperties[0]));
        case "s":
          // uniques:765|s
          return new Set(nameAndValue[0], Int32.Parse(statProperties[0]));
        default:
          throw new ArgumentOutOfRangeException("Unknown message type: " + statProperties[1]);
      }
    }

    public static bool IsProbablyAValidMessage(string line)
    {
      if (String.IsNullOrWhiteSpace(line)) return false;
      string[] nameAndValue = line.Split(':');
      if (nameAndValue[0].Length == 0) return false;
      return true;
    }
  }
}
