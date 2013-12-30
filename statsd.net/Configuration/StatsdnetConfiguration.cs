using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using log4net;
using statsd.net.core;
using statsd.net.core.Backends;
using statsd.net.shared.Services;

namespace statsd.net.Configuration
{
  public class StatsdnetConfiguration : IDisposable
  {
    private static readonly ILog _log = LogManager.GetLogger("statsd.net");

    public string Name { get; set; }
    public bool HideSystemStats { get; set; }
    public TimeSpan FlushInterval { get; set; }
    public List<ListenerConfiguration> Listeners { get; private set; }
    public Dictionary<string, XElement> BackendConfigurations { get; private set; }
    public Dictionary<string, AggregatorConfiguration> Aggregators { get; private set; }

    [ImportMany]
    public IEnumerable<IBackend> AvailableBackends { get; set; }

    private CompositionContainer _container;
    private AggregateCatalog _catalog; 

    public StatsdnetConfiguration()
    {
      Listeners = new List<ListenerConfiguration>();
      BackendConfigurations = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
      Aggregators = new Dictionary<string, AggregatorConfiguration>();

      InitializePlugins();
    }

    private void InitializePlugins()
    {
      // We look for plugins in our own assembly and in any DLLs that live next to our EXE.
      // We could force all plugins to be in a "Plugins" directory, but it seems more straightforward
      // to just leave everything in one directory
      var builtinPlugins = new AssemblyCatalog(GetType().Assembly);
      var externalPlugins = new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
      
      _catalog = new AggregateCatalog(builtinPlugins, externalPlugins);
      _container = new CompositionContainer(_catalog);

      try
      {
        _container.SatisfyImportsOnce(this);       
      }
      catch (CompositionException ex)
      {
        if (_log.IsErrorEnabled)
        {
          _log.ErrorFormat("MEF Composition Exception: {0}", ex.Message);

          var errors = String.Join("\n    ", ex.Errors.Select(x => x.Description));
          _log.ErrorFormat("Composition Errors: {0}", errors);
        }
        throw;
      }
    }

    public IEnumerable<IBackend> GetConfiguredBackends(ISystemMetricsService systemMetrics)
    {
      if (_log.IsInfoEnabled)
      {
        var availableBackendsString = String.Join(", ", AvailableBackends.Select(x => x.Name));
        _log.InfoFormat("Available Backends: {0}", availableBackendsString);
      }
      
      foreach (var pair in BackendConfigurations)
      {
        string backendName = pair.Key;
        IBackend backend = AvailableBackends.FirstOrDefault(x => x.Name.Equals(backendName, StringComparison.OrdinalIgnoreCase));

        if (backend == null)
        {
          _log.WarnFormat("Unrecognized backend configuration for \"{0}\".  Backend will be ignored.", backendName);
          continue;
        }

        backend.Configure(Name, pair.Value, systemMetrics);
        yield return backend;
      }
      
    }

    public void Dispose()
    {
      _container.Dispose();
      _container = null;
    }
  }
}
