using statsd.net.Messages;
using statsd.net.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;

namespace statsd.net.Framework
{
  public class TimedCounterAggregatorBlockFactory
  {
    public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<GraphiteLine> target,
      string rootNamespace, 
      IIntervalService intervalService)
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
          var bucketOfCounters = counters.ToArray();
          counters.Clear();
          var lines = bucketOfCounters.Select(q => new GraphiteLine(ns + q.Key, q.Value, e.Epoch)).ToArray();
          for (int i = 0; i < lines.Length; i++)
          {
            target.Post(lines[i]);
          }
        };

      incoming.Completion.ContinueWith(p =>
        {
          // Stop the timer
          intervalService.Cancel();
          // Send the last counters through
          intervalService.RunOnce();
          // Tell the upstream block that we're done
          target.Complete();
        });
      intervalService.Start();
      return incoming;
    }
  }
}
