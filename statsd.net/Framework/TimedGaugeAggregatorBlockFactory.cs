using statsd.net.Messages;
using statsd.net.Services;
using System;
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
      string rootNamespace, IIntervalService intervalService)
    {
      var gauges = new Dictionary<string, int>();
      var root = rootNamespace;
      var spinLock = new SpinLock();
      var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";

      var incoming = new ActionBlock<StatsdMessage>(p =>
        {
          bool gotLock = false;
          var gauge = p as Gauge;
          try
          {
            spinLock.Enter(ref gotLock);
            if (gauges.ContainsKey(gauge.Name))
            {
              gauges[gauge.Name] = gauge.Value;
            }
            else
            {
              gauges.Add(gauge.Name, gauge.Value);
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
          if (gauges.Count == 0)
          {
            return;
          }
          bool gotLock = false;
          Dictionary<string, int> bucketOfGauges = null;
          try
          {
            spinLock.Enter(ref gotLock);
            bucketOfGauges = gauges;
            gauges = new Dictionary<string,int>();
          }
          finally
          {
            if (gotLock) {
              spinLock.Exit(false);
            }
          }
          var lines = bucketOfGauges.Select(q => new GraphiteLine(ns + q.Key, q.Value, epoch)).ToArray();
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
