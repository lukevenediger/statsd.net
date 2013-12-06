using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace statsd.net.Configuration
{
  public static class ConfigurationFactory
  {
    public static StatsdnetConfiguration Parse(string configFile)
    {
      var config = new StatsdnetConfiguration();
      var xml = XDocument.Parse(File.ReadAllText(configFile));
      var statsdnet = xml.Element("statsdnet");
      config.Name = statsdnet.Attribute("name").Value;
      if ( statsdnet.Attributes().Any( p => p.Name == "hideSystemStats" ) )
      {
        config.HideSystemStats = statsdnet.ToBoolean( "hideSystemStats" );
      }

      // Add listeners
      foreach (var item in statsdnet.Element("listeners").Elements())
      {
        ListenerConfiguration listener = null;
        switch (item.Name.LocalName)
        {
          case "udp":
            listener = new UDPListenerConfiguration(item.ToInt("port"));
            break;
          case "tcp":
            listener = new TCPListenerConfiguration(item.ToInt("port"));
            break;
          case "http":
            listener = new HTTPListenerConfiguration(item.ToInt("port"));
            if ( item.Attribute( "headerKey" ) != null )
            {
              ( ( HTTPListenerConfiguration )listener ).HeaderKey = item.Attribute( "headerKey" ).Value;
            }
            break;
          case "statsdnet":
            listener = new StatsdnetListenerConfiguration(item.ToInt("port"));
            break;
          default:
            throw new ArgumentOutOfRangeException("Not sure what this listener is: " + item.Name);
        }
        config.Listeners.Add(listener);
      }

      // Add Backends
      foreach (var item in statsdnet.Element("backends").Elements())
      {
        BackendConfiguration backend = null;
        switch (item.Name.LocalName)
        {
          case "sqlserver":
            backend = new SqlServerConfiguration(item.Attribute("connectionString").Value, item.ToInt("writeBatchSize"));
            break;
          case "graphite":
            backend = new GraphiteConfiguration(item.Attribute("host").Value, item.ToInt("port"));
            break;
          case "console":
            backend = new ConsoleConfiguration();
            break;
          case "librato":
            backend = new LibratoBackendConfiguration(
                email: item.Attribute("email").Value,
                token: item.Attribute("token").Value,
                numRetries: item.ToInt("numRetries"),
                retryDelay: ConvertToTimespan(item.Attribute("retryDelay").Value),
                postTimeout: ConvertToTimespan(item.Attribute("postTimeout").Value),
                maxBatchSize: item.ToInt("maxBatchSize"),
                countersAsGauges: item.ToBoolean("countersAsGauges")
              );
            break;
          case "statsdnet":
            backend = new StatsdBackendConfiguration(item.Attribute("host").Value, 
              item.ToInt("port"),
              ConvertToTimespan(item.Attribute("flushInterval").Value),
              item.ToBoolean("enableCompression", true));
            break;
        }
        config.Backends.Add(backend);
      }

      // Add aggregators
      var flushInterval = ConvertToTimespan(statsdnet.Element("aggregation").Attribute("flushInterval").Value);
      config.FlushInterval = flushInterval;
      var aggregatorGroup = new AggregatorConfiguration();
      foreach (var item in statsdnet.Element("aggregation").Elements())
      {
        switch (item.Name.LocalName)
        {
          case "gauges":
            config.Aggregators.Add("gauges", new GaugeAggregatorConfig(ns: item.Attribute("namespace").Value,
                removeZeroGauges: item.ToBoolean("removeZeroGauges")));
            break;
          case "counters":
            config.Aggregators.Add("counters", new CounterAggregationConfig(ns: item.Attribute("namespace").Value));
            break;
          case "sets":
            config.Aggregators.Add("sets", new SetAggregationConfig(ns: item.Attribute("namespace").Value));
            break;
          case "timers":
            var timerConfig = new TimersAggregationConfig( ns: item.Attribute( "namespace" ).Value, calculateSumSquares: item.ToBoolean( "calculateSumSquares" ) );
            config.Aggregators.Add("timers", timerConfig);
            // Now add the percentiles
            foreach (var subItem in item.Elements())
            {
              if (!timerConfig.AddPercentile(new PercentileConfig(
                name: subItem.Attribute("name").Value,
                threshold: subItem.ToInt("threshold"),
                flushInterval: ConvertToTimespan(subItem.Attribute("flushInterval").Value)
                )))
              {
                // TODO: log that a duplicate percentile was ignored
              }
            }
            break;
        }
      }
      return config;
    }

    private static TimeSpan ConvertToTimespan(string time)
    {
      string amount = String.Empty;
      foreach (var character in time)
      {
        if (Char.IsNumber(character))
        {
          amount += character;
        }
        else if (Char.IsLetter(character))
        {
          var value = Int32.Parse(amount);
          switch(character)
          {
            case 's':
              return new TimeSpan(0, 0, value);
            case 'm':
              return new TimeSpan(0, value, 0);
            case 'h':
              return new TimeSpan(value, 0, 0);
            case 'd':
              return new TimeSpan(value, 0, 0, 0);
          }
        }
      }
      // Default to seconds if there isn't a postfix
      return new TimeSpan(0, 0, Int32.Parse(amount));
    }
  }
}
