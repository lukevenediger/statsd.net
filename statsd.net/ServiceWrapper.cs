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
using statsd.net.Configuration;

namespace statsd.net
{
  public class ServiceWrapper : ServiceBase
  {
    private Statsd _statsd;
    private string _configFile;

    public ServiceWrapper(string configFile = null)
    {
      _configFile = configFile ?? "statsdnet.config";
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
      //TODO : JV IS CONFIG FILE A ACTUAL FILE PATH?  IF SO THEN ITS MISLEADING SHOULD BE CONFIGFILEPATH??
      var configFile = ResolveConfigFile(_configFile);
      if (!File.Exists(configFile))
      {
        throw new FileNotFoundException("Could not find the statsd.net config file. I looked here: " + configFile);
      }
      var config = ConfigurationFactory.Parse(configFile);
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
