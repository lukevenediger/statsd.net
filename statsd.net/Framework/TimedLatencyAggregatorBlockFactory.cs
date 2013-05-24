using statsd.net.Messages;
using statsd.net.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;

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
          latencies.AddOrUpdate( latency.Name,
              ( key ) =>
              {
                return new ConcurrentBag<int>( new int [] { latency.ValueMS } );
              },
              ( key, bag ) =>
              {
                bag.Add( latency.ValueMS );
                return bag;
              } );
        },
        new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded } );

      intervalService.Elapsed += (sender, e) =>
        {
          if (latencies.Count == 0)
          {
            return;
          }
          var measurements = latencies.ToArray();
          latencies.Clear();
          foreach (var measurement in measurements)
          {
            target.Post(new GraphiteLine(ns + measurement.Key + ".count", measurement.Value.Count, e.Epoch));
            target.Post(new GraphiteLine(ns + measurement.Key + ".min", measurement.Value.Min(), e.Epoch));
            target.Post(new GraphiteLine(ns + measurement.Key + ".max", measurement.Value.Max(), e.Epoch));
            target.Post(new GraphiteLine(ns + measurement.Key + ".mean", Convert.ToInt32(measurement.Value.Average()), e.Epoch));
            target.Post(new GraphiteLine(ns + measurement.Key + ".sum", measurement.Value.Sum(), e.Epoch));
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
