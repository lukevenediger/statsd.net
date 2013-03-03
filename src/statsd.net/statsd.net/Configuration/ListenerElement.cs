using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration
{
  public class ListenerElement : ConfigurationElement
  {
    [ConfigurationProperty("type", IsRequired = true)]
    public string Type
    {
      get { return this["type"] as string; }
      set { this["type"] = value; }
    }

    [ConfigurationProperty("port", IsRequired=true)]
    [IntegerValidator(MinValue=0, MaxValue=65535)]
    public int Port
    {
      get { return (int)this["port"]; }
      set { this["port"] = value; }
    }

    public override string ToString()
    {
      return String.Format("{0} :{1}", Type, Port);
    }
  }
}
