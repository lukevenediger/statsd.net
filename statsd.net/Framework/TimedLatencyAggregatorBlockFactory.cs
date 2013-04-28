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

namespace statsd.net.Framework
{
  public class TimedLatencyAggregatorBlockFactory
  {
    public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<GraphiteLine> target,
      string rootNamespace, 
      IIntervalService intervalService)
    {
      var latencies = new Dictionary<string, List<int>>();
      var root = rootNamespace;
      var spinLock = new SpinLock();
      var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";
      var blockOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 };
      var busyProcessingTimerHandle = new ManualResetEvent(false);

      var incoming = new ActionBlock<StatsdMessage>(p =>
        {
          bool gotLock = false;
          var latency = p as Timing;
          try
          {
            spinLock.Enter(ref gotLock);
            if (latencies.ContainsKey(latency.Name))
            {
              latencies[latency.Name].Add(latency.ValueMS);
            }
            else
            {
              latencies.Add(latency.Name, new List<int>() { latency.ValueMS });
            }
          }
          finally
          {
            if (gotLock)
            {
              spinLock.Exit(false);
            }
          }
        },
        new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
      intervalService.Elapsed = (epoch) =>
        {
          if (latencies.Count == 0)
          {
            return;
          }
          bool gotLock = false;
          Dictionary<string, List<int>> bucketOfLatencies = null;
          try
          {
            spinLock.Enter(ref gotLock);
            bucketOfLatencies = latencies;
            latencies = new Dictionary<string, List<int>>();
          }
          finally
          {
            if (gotLock)
            {
              spinLock.Exit(false);
            }
          }
          foreach (var measurements in bucketOfLatencies)
          {
            target.Post(new GraphiteLine(ns + measurements.Key + ".count", measurements.Value.Count, epoch));
            target.Post(new GraphiteLine(ns + measurements.Key + ".min", measurements.Value.Min(), epoch));
            target.Post(new GraphiteLine(ns + measurements.Key + ".max", measurements.Value.Max(), epoch));
            target.Post(new GraphiteLine(ns + measurements.Key + ".mean", Convert.ToInt32(measurements.Value.Average()), epoch));
            target.Post(new GraphiteLine(ns + measurements.Key + ".sum", measurements.Value.Sum(), epoch));
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
