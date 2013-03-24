using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.System
{
  public class LoggingBootstrap
  {
    public static void Configure()
    {
      var configFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config"));
      XmlConfigurator.ConfigureAndWatch(configFile);
      var currentProcess = Process.GetCurrentProcess();
      GlobalContext.Properties["pid"] = currentProcess.Id.ToString();
      GlobalContext.Properties["processname"] = currentProcess.ProcessName;
    }
  }
}
