using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration
{
  public class StatsdnetConfiguration
  {
    public string Name { get; set; }
    public TimeSpan FlushInterval { get; set; }
    public List<ListenerConfiguration> Listeners { get; private set; }
    public List<BackendConfiguration> Backends { get; private set; }
    public Dictionary<string, AggregatorConfiguration> Aggregators { get; private set; }

    public StatsdnetConfiguration()
    {
      Listeners = new List<ListenerConfiguration>();
      Backends = new List<BackendConfiguration>();
      Aggregators = new Dictionary<string, AggregatorConfiguration>();
    }
  }
}
