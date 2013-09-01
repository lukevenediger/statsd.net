using log4net;
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
  public class TimedLatencyPercentileAggregatorBlockFactory
  {
    public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<Bucket> target,
      string rootNamespace, 
      IIntervalService intervalService,
      int percentile,
      string percentileName,
      ILog log,
      int maxItemsPerBucket = 1000)
    {
      var latencies = new ConcurrentDictionary<string, DatapointBox>();
      var root = rootNamespace;
      var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";
      var random = new Random();
      percentileName = "." + ( percentileName ?? ( "p" + percentile ) );

      var incoming = new ActionBlock<StatsdMessage>( p =>
        {
          var latency = p as Timing;
          latencies.AddOrUpdate(latency.Name,
              (key) =>
              {
                return new DatapointBox(maxItemsPerBucket, latency.ValueMS); 
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
          var bucket = new PercentileBucket(latencies.ToArray(),
            e.Epoch,
            rootNamespace,
            percentileName,
            percentile);
          latencies.Clear();
          target.Post(bucket);
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
