using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration
{
  public class WebConsoleElement : ConfigurationElement
  {
    [ConfigurationProperty("enabled")]
    public bool Enabled
    {
      get { return (bool)this["enabled"]; }
      set { this["enabled"] = value; } 
    }

    [ConfigurationProperty("localhostOnly")]
    public bool LocalhostOnly
    {
      get { return (bool)this["localhostOnly"]; }
      set { this["localhostOnly"] = value; } 
    }

    [ConfigurationProperty("port")]
    [IntegerValidator(MinValue=0, MaxValue=65535)]
    public int port
    {
      get { return (int)this["port"]; }
      set { this["port"] = value; }
    }
  }
}
