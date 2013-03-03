using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration
{
  public class StatsdSection : ConfigurationSection
  {
    [ConfigurationProperty("listeners", IsDefaultCollection=false)]
    [ConfigurationCollection(typeof(ListenersCollection),
      AddItemName="add",
      ClearItemsName="clear",
      RemoveItemName="remove")]
    public ListenersCollection Listeners
    {
      get { return base["listeners"] as ListenersCollection; }
    }

    [ConfigurationProperty("backends", IsDefaultCollection=false)]
    [ConfigurationCollection(typeof(BackendsCollection),
      AddItemName="add",
      ClearItemsName="clear",
      RemoveItemName="remove")]
    public BackendsCollection Backends
    {
      get { return base["backends"] as BackendsCollection; }
    }

    [ConfigurationProperty("webConsole")]
    public WebConsoleElement WebConsole
    {
      get { return base["webConsole"] as WebConsoleElement; } 
      set { base["webConsole"] = value; } 
    }
  }
}
