using log4net;
using statsd.net.shared;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net.shared.Structures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
      var gauges = new ConcurrentDictionary<string, int>();
      var root = rootNamespace;
      var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";

      var incoming = new ActionBlock<StatsdMessage>(p =>
        {
          var gauge = p as Gauge;
          gauges.AddOrUpdate(gauge.Name, gauge.Value, (key, oldValue) => gauge.Value);
        },
        Utility.UnboundedExecution());

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
