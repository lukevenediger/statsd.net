using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration.Aggregator
{
  public class TimersElement : ConfigurationElement
  {
    [ConfigurationProperty("rootNamespace", IsRequired=false, DefaultValue="stats.timers")]
    public string RootNamespace
    {
      get { return this["rootNamespace"] as string; }
      set { this["rootNamespace"] = value; }
    }

    [ConfigurationProperty("allowedAggregations", IsRequired = false, DefaultValue = "min,max,count,mean")]
    public string AllowedAggregations
    {
      get { return this["allowedAggregations"] as string; }
      set { this["allowedAggregations"] = value; }
    }

    [ConfigurationProperty("percentiles")]
    [ConfigurationCollection(typeof(PercentileElement),
      AddItemName = "add",
      ClearItemsName = "clear",
      RemoveItemName = "remove")]
    public PercentileCollection Percentiles
    {
      get { return base["percentiles"] as PercentileCollection; }
    }
  }
}
