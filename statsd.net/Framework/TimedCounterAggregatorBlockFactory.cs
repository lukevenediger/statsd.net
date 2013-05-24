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
  public class TimedCounterAggregatorBlockFactory
  {
    public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<GraphiteLine> target,
      string rootNamespace, 
      IIntervalService intervalService)
    {
      var counters = new Dictionary<string, int>();
      var root = rootNamespace;
      var spinLock = new SpinLock();
      var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";

      var incoming = new ActionBlock<StatsdMessage>(p =>
        {
          bool gotLock = false;
          var counter = p as Counter;
          try
          {
            spinLock.Enter(ref gotLock);
            if (counters.ContainsKey(counter.Name))
            {
              counters[counter.Name] += counter.Value;
            }
            else
            {
              counters.Add(counter.Name, counter.Value);
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
        new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });
      intervalService.Elapsed += (sender, e) =>
        {
          if (counters.Count == 0)
          {
            return;
          }
          bool gotLock = false;
          Dictionary<string, int> bucketOfCounters = null;
          try
          {
            spinLock.Enter(ref gotLock);
            bucketOfCounters = counters;
            counters = new Dictionary<string, int>();
          }
          finally
          {
            if (gotLock)
            {
              spinLock.Exit(false);
            }
          }
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
