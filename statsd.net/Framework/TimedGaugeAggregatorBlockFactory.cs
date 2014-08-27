using log4net;
using statsd.net.core.Structures;
using statsd.net.shared;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net.shared.Structures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.Framework
{
    public class TimedGaugeAggregatorBlockFactory
    {
        public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<Bucket> target,
          string rootNamespace,
          bool removeZeroGauges,
          IIntervalService intervalService,
          ILog log)
        {
            var queue = new ConcurrentQueue<Gauge>();
            var gauges = new Dictionary<string, int>();
            var root = rootNamespace;
            var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";

            var incoming = new ActionBlock<StatsdMessage>(p =>
              {
                  var gauge = p as Gauge;
                  bool keyExists = gauges.ContainsKey(gauge.Name);
                  switch (gauge.GaugeOperation)
                  {
                      case GaugeOperation.set:
                          if (keyExists)
                          {
                              gauges[gauge.Name] = gauge.Value;
                          }
                          else
                          {
                              gauges.Add(gauge.Name, gauge.Value);
                          }
                          break;
                      case GaugeOperation.increment:
                          if (keyExists)
                          {
                              gauges[gauge.Name] += gauge.Value;
                          }
                          else
                          {
                              gauges.Add(gauge.Name, gauge.Value);
                          }
                          break;
                      case GaugeOperation.decrement:
                          if (keyExists)
                          {
                              gauges[gauge.Name] -= gauge.Value;
                              if (gauges[gauge.Name] < 0)
                              {
                                  gauges[gauge.Name] = 0;
                              }
                          }
                          else
                          {
                              gauges[gauge.Name] = 0;
                          }
                          break;
                  }
              },
              Utility.OneAtATimeExecution()
            );

            intervalService.Elapsed += (sender, e) =>
              {
                  if (gauges.Count == 0)
                  {
                      return;
                  }
                  var items = gauges.ToArray();
                  var bucket = new GaugesBucket(items, e.Epoch, ns);
                  if (removeZeroGauges)
                  {
                      // Get all zero-value gauges
                      int placeholder;
                      var zeroGauges = 0;
                      for (int index = 0; index < items.Length; index++)
                      {
                          if (items[index].Value == 0)
                          {
                              gauges.TryRemove(items[index].Key, out placeholder);
                              zeroGauges += 1;
                          }
                      }
                      if (zeroGauges > 0)
                      {
                          log.InfoFormat("Removed {0} empty gauges.", zeroGauges);
                      }
                  }

                  gauges.Clear();
                  gauges2.Clear();
                  target.Post(bucket);
              };

            incoming.Completion.ContinueWith(p =>
              {
                  // Tell the upstream block that we're done
                  target.Complete();
              });
            return incoming;
        }

        /// <summary>
        /// Stores the value of a gauge, as well as the index.
        /// </summary>
        [DebuggerDisplay("[{index}]: {gauge}")]
        private class GaugeValue
        {
            public int gauge;
            public long index;

            public GaugeValue(int gauge, long index)
            {
                this.gauge = gauge;
                this.index = index;
            }
        }
    }
}
