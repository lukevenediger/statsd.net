using statsd.net.Listeners;
using statsd.net.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Topshelf;

namespace statsd.net
{
  public class ServiceWrapper : ServiceControl
  {
    private Statsd _statsd;

    public ServiceWrapper()
    {
    }

    public bool Start(HostControl hostControl)
    {
      var configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "statsd.toml");
      var contents = File.ReadAllText(configFile);
      var config = Toml.Toml.Parse(contents);

      _statsd = new Statsd(config);
      return true;
    }

    public bool Stop(HostControl hostControl)
    {
      _statsd.Stop();
      return true;
    }
  }
}
