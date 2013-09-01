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
using log4net;
using statsd.net.shared.Structures;

namespace statsd.net.Framework
{
  public class TimedSetAggregatorBlockFactory
  {
    public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<SetsBucket> target,
      string rootNamespace, 
      IIntervalService intervalService,
      ILog log)
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
          var rawData = sets.ToArray();
          sets.Clear();
          var bucket = new SetsBucket(
            rawData.Select(p =>
              new KeyValuePair<string, List<KeyValuePair<int, bool>>>(p.Key, p.Value.ToList())
            ).ToList(),
            e.Epoch,
            ns);

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
