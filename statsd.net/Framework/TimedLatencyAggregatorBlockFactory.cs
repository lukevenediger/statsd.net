using statsd.net.shared.Messages;
using statsd.net.shared.Services;
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
      IIntervalService intervalService)
    {
      var latencies = new ConcurrentDictionary<string, ConcurrentBag<int>>();
      var root = rootNamespace;
      var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";
	  
      var incoming = new ActionBlock<StatsdMessage>( p =>
        {
          var latency = p as Timing;

          latencies.AddOrUpdate(latency.Name,
              (key) =>
              {
                return new ConcurrentBag<int>(new int[] { latency.ValueMS });
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

          var bucket = latencies.ToArray();
          latencies.Clear();

          foreach (var measurements in bucket)
          {
            var values = measurements.Value.ToArray();
            target.Post(new GraphiteLine(ns + measurements.Key + ".count", values.Length, e.Epoch));
            target.Post(new GraphiteLine(ns + measurements.Key + ".min", values.Min(), e.Epoch));
            target.Post(new GraphiteLine(ns + measurements.Key + ".max", values.Max(), e.Epoch));
            target.Post(new GraphiteLine(ns + measurements.Key + ".mean", Convert.ToInt32(values.Average()), e.Epoch));
            target.Post(new GraphiteLine(ns + measurements.Key + ".sum", values.Sum(), e.Epoch));
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
