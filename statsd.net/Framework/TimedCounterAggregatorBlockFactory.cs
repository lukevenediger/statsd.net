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
  public class TimedCounterAggregatorBlockFactory
  {
    public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<GraphiteLine> target,
      string rootNamespace, 
      IIntervalService intervalService,
      ILog log)
    {
      var counters = new ConcurrentDictionary<string, int>();
      var root = rootNamespace;
      var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";

      var incoming = new ActionBlock<StatsdMessage>(p =>
        {
          var counter = p as Counter;
          counters.AddOrUpdate(counter.Name, counter.Value, (key, oldValue) => oldValue + counter.Value);
        },
        new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });

      intervalService.Elapsed += (sender, e) =>
        {
          if (counters.Count == 0)
          {
            return;
          }

          var bucket = counters.ToArray();
          counters.Clear();
          var lines = bucket.Select(q => new GraphiteLine(ns + q.Key, q.Value, e.Epoch)).ToArray();
          for (int i = 0; i < lines.Length; i++)
          {
            target.Post(lines[i]);
          }
          log.InfoFormat("TimedCounterAggregatorBlock - Posted {0} lines", lines.Length);
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
