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
  public class TimedLatencyAggregatorBlockFactory
  {
    public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<GraphiteLine> target,
      string rootNamespace, 
      IIntervalService intervalService,
      int maxItemsPerBucket = 1000)
    {
      var latencies = new ConcurrentDictionary<string, LatencyBucket>();
      var root = rootNamespace;
      var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";
	  
      var incoming = new ActionBlock<StatsdMessage>( p =>
        {
          var latency = p as Timing;

          latencies.AddOrUpdate(latency.Name,
              (key) =>
              {
                return new LatencyBucket(maxItemsPerBucket, latency.ValueMS);
              },
              (key, bag) =>
              {
                bag.Add(latency.ValueMS);
                return bag;
              });
        },
        new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });
      
      intervalService.Elapsed += (sender, e) =>
        {
          if (latencies.Count == 0)
          {
            return;
          }

          var buckets = latencies.ToArray();
          latencies.Clear();

          foreach (var bucket in buckets)
          {
            target.Post(new GraphiteLine(ns + bucket.Key + ".count", bucket.Value.Count, e.Epoch));
            target.Post(new GraphiteLine(ns + bucket.Key + ".min", bucket.Value.Min, e.Epoch));
            target.Post(new GraphiteLine(ns + bucket.Key + ".max", bucket.Value.Max, e.Epoch));
            target.Post(new GraphiteLine(ns + bucket.Key + ".mean", bucket.Value.Mean, e.Epoch));
            target.Post(new GraphiteLine(ns + bucket.Key + ".sum", bucket.Value.Sum, e.Epoch));
          }
        };

      incoming.Completion.ContinueWith(p =>
        {
          // Stop the timer
          intervalService.Cancel();
          // Tell the upstream block that we're done
          target.Complete();
        });
      intervalService.Start();
      return incoming;
    }
  }
}
