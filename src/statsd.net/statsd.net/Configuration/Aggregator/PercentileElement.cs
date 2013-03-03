using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration.Aggregator
{
  public class PercentileElement : ConfigurationElement
  {
    [ConfigurationProperty("name", IsRequired=true, IsKey=true)]
    public string Name
    {
      get { return this["name"] as string; }
      set { this["name"] = value; }
    }

    [ConfigurationProperty("percentile", IsRequired = true)]
    [IntegerValidator(MinValue=1, MaxValue=100)]
    public int Percentile
    {
      get { return (int)this["percentile"]; }
      set { this["percentile"] = value; }
    }

    [ConfigurationProperty("flushIntervalSeconds", IsRequired=true)]
    [IntegerValidator(MinValue=1)]
    public int FlushIntervalSeconds
    {
      get { return (int)this["flushIntervalSeconds"]; }
      set { this["flushIntervalSeconds"] = value; }
    }
  }
}
