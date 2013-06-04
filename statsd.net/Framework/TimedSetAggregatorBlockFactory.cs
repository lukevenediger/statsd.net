using statsd.net.shared.Messages;
using statsd.net.shared.Services;
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
  public class TimedSetAggregatorBlockFactory
  {
    public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<GraphiteLine> target,
      string rootNamespace, 
      IIntervalService intervalService)
    {
      var sets = new ConcurrentDictionary<string, ConcurrentDictionary<int, bool>>();
      var root = rootNamespace;
      var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";

      var incoming = new ActionBlock<StatsdMessage>(p =>
        {
          var set = p as Set;
          sets.AddOrUpdate(set.Name, 
            (key) =>
              {
                var setDict = new ConcurrentDictionary<int, bool>();
                setDict.AddOrUpdate( set.Value, ( key2 ) => true, ( key2, oldValue ) => true );
                return setDict;
              },
            (key, setDict) =>
              {
                setDict.AddOrUpdate( set.Value, ( key2 ) => true, ( key2, oldValue ) => true );
                return setDict;
              });
        },
        new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });

      intervalService.Elapsed += (sender, e) =>
        {
          if (sets.Count == 0)
          {
            return;
          }
          var bucketOfSets = sets.ToArray();
          sets.Clear();
          foreach (var bucket in bucketOfSets)
          {
            foreach ( var set in bucket.Value )
            {
              target.Post( new GraphiteLine( ns + bucket.Key + "." + set.Key.ToString(), 1, e.Epoch ) );
            }
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
