using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration
{
  public class BackendElement : ConfigurationElement
  {
    [ConfigurationProperty("type", IsRequired=true)]
    public string Type
    {
      get { return this["type"] as string; }
      set { this["type"] = value; } 
    }

    public string Host
    {
      get { return this["host"] as string; }
      set { this["host"] = value; }
    }

    public int Port
    {
      get { return (int)this["port"]; }
      set { this["port"] = value; }
    }

    public string ConnectionString
    {
      get { return this["connectionString"] as string; }
      set { this["connectionString"] = value; }
    }

    public override string ToString()
    {
      return String.Format("{0} {1} {2} {3}",
        Type,
        Host,
        Port,
        ConnectionString);
    }
  }
}
