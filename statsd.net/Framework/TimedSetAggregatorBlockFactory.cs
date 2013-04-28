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
  public class TimedSetAggregatorBlockFactory
  {
    public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<GraphiteLine> target,
      string rootNamespace, 
      IIntervalService intervalService)
    {
      var sets = new Dictionary<string, Dictionary<int, bool>>();
      var root = rootNamespace;
      var spinLock = new SpinLock();
      var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";
      var blockOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 };
      var busyProcessingTimerHandle = new ManualResetEvent(false);

      var incoming = new ActionBlock<StatsdMessage>(p =>
        {
          bool gotLock = false;
          var set = p as Set;
          try
          {
            spinLock.Enter(ref gotLock);
            if (sets.ContainsKey(set.Name))
            {
              if (!sets[set.Name].ContainsKey(set.Value))
              {
                sets[set.Name].Add(set.Value, true);
              }
            }
            else
            {
              sets.Add(set.Name, new Dictionary<int, bool>() { {set.Value, true} });
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
          if (sets.Count == 0)
          {
            return;
          }
          bool gotLock = false;
          Dictionary<string, Dictionary<int, bool>> bucketOfSets = null;
          try
          {
            spinLock.Enter(ref gotLock);
            bucketOfSets = sets;
            sets = new Dictionary<string, Dictionary<int, bool>>();
          }
          finally
          {
            if (gotLock)
            {
              spinLock.Exit(false);
            }
          }
          foreach (var measurements in bucketOfSets)
          {
            target.Post(new GraphiteLine(ns + measurements.Key, measurements.Value.Count));
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
