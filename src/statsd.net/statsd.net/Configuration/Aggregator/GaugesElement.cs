using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration.Aggregator
{
  public class GaugesElement : ConfigurationElement
  {
    [ConfigurationProperty("rootNamespace", IsRequired=false, DefaultValue="stats.gauges")]
    public string RootNamespace
    {
      get { return this["rootNamespace"] as string; }
      set { this["rootNamespace"] = value; }
    }
  }
}
