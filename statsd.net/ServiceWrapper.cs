using statsd.net.shared.Listeners;
using statsd.net.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net
{
  public class ServiceWrapper : ServiceBase
  {
    private Statsd _statsd;

    public ServiceWrapper()
    {
    }

    protected override void OnStart(string[] args)
    {
      Start(false);
    }

    protected override void OnStop()
    {
      if ( _statsd != null )
      {
        _statsd.Stop();
        _statsd.ShutdownWaitHandle.WaitOne();
      }
    }

    public void Start(bool waitForCompletion = true)
    {
      var configFile = ResolveConfigFile("statsdnet.toml");
      if (!File.Exists(configFile))
      {
        configFile = ResolveConfigFile("statsd.toml");
      }
      if (!File.Exists(configFile))
      {
        throw new FileNotFoundException("Could not find the statsd.net config file. Tried statsdnet.toml and statsd.toml.");
      }
      var contents = File.ReadAllText(configFile);
      var config = Toml.Toml.Parse(contents);

      _statsd = new Statsd(config);
      if (waitForCompletion)
      {
        _statsd.ShutdownWaitHandle.WaitOne();
      }
    }

    private static string ResolveConfigFile(string filename)
    {
      return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), filename);
    }
  }
}
