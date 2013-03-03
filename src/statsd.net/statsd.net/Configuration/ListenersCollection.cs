using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration
{
  public class ListenersCollection : ConfigurationElementCollection
  {
    protected override ConfigurationElement CreateNewElement()
    {
      return new ListenerElement();
    }

    protected override object GetElementKey(ConfigurationElement element)
    {
      return element.ToString();
    }
  }
}
