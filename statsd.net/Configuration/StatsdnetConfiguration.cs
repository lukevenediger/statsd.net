using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using statsd.net.shared.Backends;

namespace statsd.net.Configuration
{
  public class StatsdnetConfiguration
  {
    public string Name { get; set; }
    public bool HideSystemStats { get; set; }
    public TimeSpan FlushInterval { get; set; }
    public List<ListenerConfiguration> Listeners { get; private set; }
    public Dictionary<string, XElement> BackendConfigurations { get; private set; }
    public Dictionary<string, AggregatorConfiguration> Aggregators { get; private set; }

    public StatsdnetConfiguration()
    {
      Listeners = new List<ListenerConfiguration>();
      BackendConfigurations = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
      Aggregators = new Dictionary<string, AggregatorConfiguration>();
    }
  }
}
