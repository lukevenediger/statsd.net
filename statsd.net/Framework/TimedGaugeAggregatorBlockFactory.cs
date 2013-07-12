using log4net;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
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
    public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<GraphiteLine> target,
      string rootNamespace, 
      bool deleteGaugesOnFlush,
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
        new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });

      intervalService.Elapsed += (sender, e) =>
        {
          if (gauges.Count == 0)
          {
            return;
          }
          var bucket = gauges.ToArray();
          if (deleteGaugesOnFlush)
          {
          }
          var lines = bucket.Select(q => new GraphiteLine(ns + q.Key, q.Value, e.Epoch)).ToArray();
          for (int i = 0; i < lines.Length; i++)
          {
            target.Post(lines[i]);
          }
          log.InfoFormat("TimedGaugeAggregatorBlock - Posted {0} lines.", lines.Length);
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
