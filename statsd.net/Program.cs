using statsd.net.shared.Listeners;
using statsd.net.Framework;
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
  public class Program
  {
    static void Main(string[] args)
    {
      if (Environment.UserInteractive)
      {
        string action = args.Length >= 1 ? args[0] : "";

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
            RunConsoleMode(args.Length == 2 ? args[1] : null);
            break;
          case "--help":
            PrintHelp();
            break;
          default:
#if DEBUG
            if ( global::System.Diagnostics.Debugger.IsAttached )
            {
              RunConsoleMode();
            }
#endif
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
      C("  --install              Install statsd.net as a Windows Service.");
      C("  --uninstall            Uninstall statsd.net");
      C("  --console              Run statsd.net in console mode (does not need to be installed first)");
      C("  --console CONFIG_FILE  Run statsd.net in console mode, using the specified configuration file.");
      C("  --version              Prints the service version");
      C("  --help                 Prints this help information.");
    }

    private static void InstallService()
    {
      try
      {
        ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
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

    private static void RunConsoleMode(string configFile = null)
    {
      var service = new ServiceWrapper(configFile);
      Console.CancelKeyPress += (source, args) =>
        {
          Console.WriteLine("CTRL^C pressed, shutting down...");
          service.Stop();
        };
      service.Start();
      Console.WriteLine("Press CTRL^C to shut down.");
    }
  }
}
