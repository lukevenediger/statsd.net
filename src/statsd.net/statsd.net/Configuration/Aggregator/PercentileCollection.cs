using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration.Aggregator
{
  public class PercentileCollection : ConfigurationElementCollection
  {
    protected override ConfigurationElement CreateNewElement()
    {
      return new PercentileElement();
    }

    protected override object GetElementKey(ConfigurationElement element)
    {
      return ((PercentileElement)element).Name;
    }
  }
}
