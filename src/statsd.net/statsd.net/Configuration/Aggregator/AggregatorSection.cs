using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration.Aggregator
{
  public class AggregatorSection : ConfigurationSection
  {
    [ConfigurationProperty("flushIntervalSeconds", IsRequired = true)]
    [IntegerValidator(MinValue = 1)]
    public int FlushIntervalSeconds
    {
      get { return (int)this["flushIntervalSeconds"]; }
      set { this["flushIntervalSeconds"] = value; }
    }

    [ConfigurationProperty("gauges")]
    public GaugesElement Gauges
    {
      get { return (GaugesElement)this["gauges"]; }
      set { this["gauges"] = value; } 
    }

    [ConfigurationProperty("gauges")]
    public CountersElement Counters
    {
      get { return (CountersElement)this["counters"]; }
      set { this["counters"] = value; } 
    }

    [ConfigurationProperty("timers")]
    public TimersElement Timers
    {
      get { return (TimersElement)this["timers"]; }
      set { this["timers"] = value; } 
    }
  }
}
