using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.System
{
  [RunInstaller(true)]
  public partial class ProjectInstaller : Installer
  {
    public ProjectInstaller()
    {
      var installer = new ServiceInstaller();
      installer.StartType = ServiceStartMode.Automatic;
      installer.ServiceName = "Statsd.net";
      installer.Description = "Data collection and aggregation service for Graphite. Read more about it at https://github.com/lukevenediger/statsd.net/";

      var processInstaller = new ServiceProcessInstaller();
      processInstaller.Account = ServiceAccount.LocalService;
      Installers.Add(installer);
      Installers.Add(processInstaller);
    }
  }
}
