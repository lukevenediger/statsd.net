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

    public static StatsdMessage ParseMessage(string line)
    {
      try
      {
        string[] nameAndValue = line.Split(':');
        if (nameAndValue[0].Length == 0)
        {
          return new InvalidMessage("Name cannot be empty.");
        }
        string[] statProperties = nameAndValue[1].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
        if (statProperties.Length < 2)
        {
          return new InvalidMessage("Malformed message.");
        }

        switch (statProperties[1])
        {
          case "c":
            if (statProperties.Length == 2)
            {
              // gorets:1|c
              return new Counter(nameAndValue[0], Double.Parse(statProperties[0]));
            }
            else
            {
              // gorets:1|c|@0.1
              return new Counter(nameAndValue[0], Double.Parse(statProperties[0]), float.Parse(statProperties[2].Remove(0, 1)));
            }
          case "ms":
            // glork:320|ms
            return new Timing(nameAndValue[0], Double.Parse(statProperties[0]));
          case "g":
            // gaugor:333|g
            return new Gauge(nameAndValue[0], Double.Parse(statProperties[0]));
          case "s":
            // uniques:765|s
            // uniques:ABSA434As1|s
            return new Set(nameAndValue[0], statProperties[0]);
          case "r":
            // some.other.value:12312|r
            // some.other.value:12312|r|99988883333
            if (statProperties.Length == 2)
            {
              return new Raw(nameAndValue[0], Double.Parse(statProperties[0]));
            }
            else
            {
              return new Raw(nameAndValue[0], Double.Parse(statProperties[0]), long.Parse(statProperties[2]));
            }
          case "cg":
            // calendargram.key:value|cg|{h,d,w,m,dow}
            return new Calendargram(nameAndValue[0], statProperties[0], statProperties[2]);
          default:
            return new InvalidMessage("Unknown message type: " + statProperties[1]);
        }
      }
      catch (Exception ex)
      {
        return new InvalidMessage("Couldn't parse message: " + ex.Message);
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
