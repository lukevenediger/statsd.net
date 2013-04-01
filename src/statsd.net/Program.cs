using statsd.net.Listeners;
using statsd.net.System;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net
{
  class Program
  {
    static void Main(string[] args)
    {
      if (Environment.UserInteractive)
      {
        SuperCheapIOC.Add(new ConsoleLogger());
        string action = String.Concat(args);
        switch (action)
        {
          case "--install":
            InstallService();
            break;
          case "--uninstall":
            UninstallService();
            break;
          case "--version":
            PrintVersion();
            break;
          case "--console":
            RunConsoleMode();
            break;
          case "--help":
            PrintHelp();
            break;
          default:
            PrintHelp(action);
            Environment.Exit(1);
            break;
        }
        Environment.Exit(0);
      }
      else
      {
        ServiceBase.Run(new ServiceWrapper());
      }
    }

    private static void PrintHelp(string action = null)
    {
      Action<string> C = (input) => { Console.WriteLine(input); };
      if (action != null)
      {
        C("Error - unknown option: " + action);
      }
      C("Usage: statsd.net.exe [ --install | --uninstall | --console | --version | --help ]");
      C("  --install     Install statsd.net as a Windows Service.");
      C("  --uninstall   Uninstall statsd.net");
      C("  --console     Run statsd.net in console mode (does not need to be installed first)");
      C("  --version     Prints the service version");
      C("  --help        Prints this help information.");
    }

    private static void InstallService()
    {
      try
      {
        ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
        var eventLog = new EventLogLogger();
        eventLog.CheckEventSource();
        Console.WriteLine("Service installed successfully (don't forget to start it!)");
      }
      catch (Exception ex)
      {
        Console.WriteLine("Could not install the service: " + ex.Message);
        Console.WriteLine(ex.ToString());
      }
    }

    private static void UninstallService()
    {
      try
      {
        ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
        new EventLogLogger().DiscardEventSource();
        Console.WriteLine("Service uninstalled successfully.");
      }
      catch (Exception ex)
      {
        Console.WriteLine("Could not uninstall the service: " + ex.Message);
        Console.WriteLine(ex.ToString());
      }
    }

    private static void PrintVersion()
    {
      Console.WriteLine("Statsd.net v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
    }

    private static void RunConsoleMode()
    {
      var service = new ServiceWrapper();
      Console.CancelKeyPress += (source, args) =>
        {
          Console.WriteLine("CTRL^C pressed, shutting down...");
          service.Stop();
        };
      Console.WriteLine("Press CTRL^C to shut down.");
      service.Start();
    }
  }
}
