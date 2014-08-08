using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;
using log4net;
using statsd.net.shared.Structures;

namespace statsd.net.Framework
{
    public class TimedCalendargramAggregatorBlockFactory
    {
        public static readonly char[] UNDERSCORE = new char[] { '_' };
        public const string METRIC_IDENTIFIER_SEPARATOR = "^ ^";
        public static readonly string[] METRIC_IDENTIFIER_SEPARATOR_SPLITTER = new String[] { "^ ^" };

        public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<CounterBucket> target,
          string rootNamespace,
          IIntervalService intervalService,
          ITimeWindowService timeWindowService,
          ILog log)
        {
            var windows = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();
            var root = rootNamespace;
            var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";

            var incoming = new ActionBlock<StatsdMessage>(p =>
              {
                  var calendargram = p as Calendargram;
                  var metricName = calendargram.Name + METRIC_IDENTIFIER_SEPARATOR + calendargram.Value;

                  var period = timeWindowService.GetTimeWindow().GetTimePeriod(calendargram.Period);
                  windows.AddOrUpdate(period,
                    (key) =>
                    {
                        var window = new ConcurrentDictionary<string, int>();
                        window.AddOrUpdate(metricName, (key2) => 1, (key2, oldValue) => oldValue + 1);
                        return window;
                    },
                    (key, window) =>
                    {
                        window.AddOrUpdate(metricName, (key2) => 1, (key2, oldValue) => oldValue + 1);
                        return window;
                    }
                  );
              },
              new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });

            intervalService.Elapsed += (sender, e) =>
              {
                  if (windows.Count == 0)
                  {
                      return;
                  }
                  var currentTimeWindow = timeWindowService.GetTimeWindow();

                  var periodsNotPresent = windows
                      .ToArray()
                      .Where(p => !currentTimeWindow.AllPeriods.Contains(p.Key))
                      .Select(p => p.Key);

                  CounterBucket bucket;

                  foreach (var period in periodsNotPresent)
                  {
                      ConcurrentDictionary<String, int> window;
                      if (windows.TryRemove(period, out window))
                      {
                          var parts = period.Split(UNDERSCORE);
                          var qualifier = "." + parts[0] + "." + parts[1];

                          var metricsAndValues = window.ToArray();
                          var metrics = new Dictionary<String, int>();
                          for (int index = 0; index < metricsAndValues.Length; index++)
                          {
                              var metricName = metricsAndValues[index].Key.Split(
                                  METRIC_IDENTIFIER_SEPARATOR_SPLITTER, 
                                  StringSplitOptions.RemoveEmptyEntries
                              )[0] + qualifier;

                              if (metrics.ContainsKey(metricName))
                              {
                                  metrics[metricName] += 1;
                              }
                              else
                              {
                                  metrics[metricName] = 1;
                              }
                          }

                          var metricList = metrics.Select(metric =>
                          {
                              return new KeyValuePair<string, int>(
                                metric.Key,
                                metric.Value
                              );
                          }).ToArray();
                          bucket = new CounterBucket(metricList, e.Epoch, ns);
                          target.Post(bucket);
                      }
                  }
              };
            incoming.Completion.ContinueWith(p =>
              {
                  log.Info("TimedCounterAggregatorBlock completing.");
                  // Tell the upstream block that we're done
                  target.Complete();
              });
            return incoming;
        }
    }
}
