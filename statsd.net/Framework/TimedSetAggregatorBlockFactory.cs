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
    public class TimedSetAggregatorBlockFactory
    {
        public static readonly char[] UNDERSCORE = new char[] { '_' };

        public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<CounterBucket> target,
          string rootNamespace,
          IIntervalService intervalService,
          ILog log)
        {
            var sets = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();
            var windows = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();
            var root = rootNamespace;
            var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";
            var timeWindow = new SetTimeWindow();

            var incoming = new ActionBlock<StatsdMessage>(p =>
              {
                  var set = p as Set;
                  var metricName = set.Name + "." + set.Value;

                  foreach (var period in timeWindow.AllPeriods)
                  {
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
                  }
              },
              new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });

            intervalService.Elapsed += (sender, e) =>
              {
                  if (sets.Count == 0)
                  {
                      return;
                  }
                  var currentTimeWindow = new SetTimeWindow();
                  var periodsNotPresent = currentTimeWindow.GetDifferences(timeWindow);
                  // Make the current time window the one we use to manage the collections
                  timeWindow = currentTimeWindow;
                  CounterBucket bucket;

                  foreach (var period in periodsNotPresent)
                  {
                      ConcurrentDictionary<String, int> window;
                      if (windows.TryRemove(period, out window))
                      {
                          var parts = period.Split(UNDERSCORE);
                          var qualifier = "." + parts[0] + "." + parts[1];
                          var metrics = window.ToArray().Select(metric =>
                              {
                                  return new KeyValuePair<string, int>(
                                    metric.Key + qualifier,
                                    metric.Value
                                  );
                              }).ToArray();
                          bucket = new CounterBucket(metrics, e.Epoch, ns);
                          target.Post(bucket);
                          break;
                      }
                  }
              };
            incoming.Completion.ContinueWith(p =>
              {
                  // Tell the upstream block that we're done
                  target.Complete();
              });
            return incoming;
        }
    }
}
