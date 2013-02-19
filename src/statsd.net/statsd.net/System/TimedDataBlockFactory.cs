using statsd.net.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;

namespace statsd.net.System
{
  public class TimedDataBlockFactory
  {
    public static IPropagatorBlock<StatsdMessage, GraphiteLine[]> CreateTimedBlock(TimeSpan collectionTime)
    {
      var outgoing = new BufferBlock<GraphiteLine[]>();
      var gauges = new ConcurrentDictionary<string, int>();
      var counters = new ConcurrentDictionary<string, int>();
      var timings = new ConcurrentDictionary<string, ConcurrentBag<int>>();
      var busyUpdatingGauges = false;
      var busyUpdatingCounters = false;
      var busyUpdatingTimings = false;

      var incoming = new ActionBlock<StatsdMessage>(item =>
        {
          switch (item.MessageType)
          {
            case MessageType.Gauge:
              Gauge gauge = item as Gauge;
              if (busyUpdatingGauges) { Monitor.Wait(gauges); }
              gauges.AddOrUpdate(gauge.Name, gauge.Value, (_1, _2) => gauge.Value);
              break;
            case MessageType.Counter:
              Counter counter = item as Counter;
              if (busyUpdatingCounters) { Monitor.Wait(counters); }
              counters.AddOrUpdate(counter.Name, counter.Value, (_1, oldValue) => oldValue + counter.Value);
              break;
            case MessageType.Timing:
              Timing timing = item as Timing;
              if (busyUpdatingTimings) { Monitor.Wait(timings); }
              timings.AddOrUpdate(timing.Name,
                (key) =>
                {
                  var newBag = new ConcurrentBag<int>();
                  newBag.Add(timing.ValueMS);
                  return newBag;
                },
                (key, oldValue) =>
                {
                  // Not sure I want to be returning the ConcurrentBag<T> each time
                  // This smells :(
                  oldValue.Add(timing.ValueMS);
                  return oldValue;
                });
              break;
          }
        },
        new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = ExecutionDataflowBlockOptions.Unbounded });
      var intervalHandle = Utility.SetInterval(collectionTime, () =>
        {
          var epoch = Utility.GetEpoch();
          KeyValuePair<string, int>[] bucket = null;
          // Lock on gauges so that we can grab the current bucket
          // and prevent incoming data from being lost as we clear it
          // out.
          lock (gauges)
          {
            busyUpdatingGauges = true;
            bucket = gauges.ToArray();
            gauges.Clear();
            busyUpdatingGauges = false;
            Monitor.Pulse(gauges);
          }

          var graphiteLines = bucket.Select(p => { return new GraphiteLine(p.Key, p.Value, epoch); })
            .ToArray();
          
          // Send the gauges on their way
          outgoing.SendAsync(graphiteLines);
        });
      incoming.Completion.ContinueWith(_3 =>
        {
          // Stop the timer;
          intervalHandle.Cancel();
          // Process the last bucket
          if (gauges.Count > 0)
          {
            var epoch = Utility.GetEpoch();
            var graphiteLines = gauges.ToArray().Select(p => { return new GraphiteLine(p.Key, p.Value, epoch); })
              .ToArray();
            // Send the gauges on their way
            outgoing.Post(graphiteLines);
          }
          // signal our completion to the upstream consumers
          outgoing.Complete();
        });
      return DataflowBlock.Encapsulate(incoming, outgoing);
    }
  }
}
