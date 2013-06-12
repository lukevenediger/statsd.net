using statsd.relay;
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

namespace statsd.relay
{
  public class ServiceWrapper : ServiceBase
  {
    private Relay _relay;

    public ServiceWrapper()
    {
    }

    protected override void OnStart(string[] args)
    {
      Start(false);
    }

    protected override void OnStop()
    {
      _relay.Stop();
      _relay.ShutdownWaitHandle.WaitOne();
    }

    public void Start(bool waitForCompletion = true)
    {
      var configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "statsd-relay.toml");
      var contents = File.ReadAllText(configFile);
      var config = Toml.Toml.Parse(contents);

      _relay = new Relay(config);
      if (waitForCompletion)
      {
        _relay.ShutdownWaitHandle.WaitOne();
      }
    }
  }
}
