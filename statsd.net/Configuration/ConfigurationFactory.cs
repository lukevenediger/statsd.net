using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using statsd.net.shared;

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
            if (statsdnet.Attributes().Any(p => p.Name == "hideSystemStats"))
            {
                config.HideSystemStats = statsdnet.ToBoolean("hideSystemStats");
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
                        if (item.Attribute("headerKey") != null)
                        {
                            ((HTTPListenerConfiguration)listener).HeaderKey = item.Attribute("headerKey").Value;
                        }
                        break;
                    case "statsdnet":
                        listener = new StatsdnetListenerConfiguration(item.ToInt("port"));
                        break;
                    case "mssql-relay":
                        listener = new MSSQLRelayListenerConfiguration(item.Attribute("connectionString").Value,
                            item.ToInt("batchSize"),
                            item.ToBoolean("deleteAfterSend"),
                            item.ToTimeSpan("pollInterval"));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Not sure what this listener is: " + item.Name);
                }
                config.Listeners.Add(listener);
            }

            // Add Backends
            foreach (var item in statsdnet.Element("backends").Elements())
            {
                string name = item.Name.LocalName;
                config.BackendConfigurations[name] = item;
            }

            // Add aggregators
            var flushInterval = statsdnet.Element("aggregation").ToTimeSpan("flushInterval");
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
                    case "calendargrams":
                        config.Aggregators.Add("calendargrams", new CalendargramAggregationConfig(ns: item.Attribute("namespace").Value));
                        break;
                    case "timers":
                        var timerConfig = new TimersAggregationConfig(ns: item.Attribute("namespace").Value, calculateSumSquares: item.ToBoolean("calculateSumSquares"));
                        config.Aggregators.Add("timers", timerConfig);
                        // Now add the percentiles
                        foreach (var subItem in item.Elements())
                        {
                            if (!timerConfig.AddPercentile(new PercentileConfig(
                              name: subItem.Attribute("name").Value,
                              threshold: subItem.ToInt("threshold"),
                              flushInterval: subItem.ToTimeSpan("flushInterval")
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
    }
}
