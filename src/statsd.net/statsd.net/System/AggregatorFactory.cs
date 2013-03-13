using statsd.net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.System
{
  public class AggregatorFactory
  {
    public static IPropagatorBlock<StatsdMessage, GraphiteLine> CreateTimedCountersBlock(string rootNamespace, TimeSpan flushPeriod)
    {
      var counters = new Dictionary<string, int>();
      var root = rootNamespace;
      var spinLock = new SpinLock();
      var outgoing = new BufferBlock<GraphiteLine>();
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
            if (gotLock) {
              spinLock.Exit(false);
            }
          }
        });
      var intervalHandle = Utility.SetInterval(flushPeriod, () =>
        {
          if (counters.Count == 0)
          {
            return;
          }
          var epoch = Utility.GetEpoch();
          bool gotLock = false;
          Dictionary<string, int> bucketOfCounters = null;
          try
          {
            spinLock.Enter(ref gotLock);
            bucketOfCounters = counters;
            counters = new Dictionary<string,int>();
          }
          finally
          {
            if (gotLock) {
              spinLock.Exit(false);
            }
          }
          var lines = bucketOfCounters.Select(q => new GraphiteLine(ns + q.Key, q.Value, epoch)).ToArray();
          for (int i = 0; i < lines.Length; i++)
          {
            outgoing.Post(lines[i]);
          }
        });
      incoming.Completion.ContinueWith(p =>
        {
          // Stop the timer
          intervalHandle.Cancel();
          // Send the last counters through
          intervalHandle.RunOnce();
          // Tell the upstream block that we're done
          outgoing.Complete();
        });
      return DataflowBlock.Encapsulate(incoming, outgoing);
    }

    public static IPropagatorBlock<StatsdMessage, GraphiteLine> CreateTimedGaugesBlock(string rootNamespace, TimeSpan flushPeriod)
    {
      var gauges = new Dictionary<string, int>();
      var root = rootNamespace;
      var spinLock = new SpinLock();
      var outgoing = new BufferBlock<GraphiteLine>();
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
            if (gotLock) {
              spinLock.Exit(false);
            }
          }
        });
      var intervalHandle = Utility.SetInterval(flushPeriod, () =>
        {
          if (gauges.Count == 0)
          {
            return;
          }
          var epoch = Utility.GetEpoch();
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
            outgoing.Post(lines[i]);
          }
        });
      incoming.Completion.ContinueWith(p =>
        {
          // Stop the timer
          intervalHandle.Cancel();
          // Send the last counters through
          intervalHandle.RunOnce();
          // Tell the upstream block that we're done
          outgoing.Complete();
        });
      return DataflowBlock.Encapsulate(incoming, outgoing);
    }

    public static IPropagatorBlock<StatsdMessage, GraphiteLine> CreateTimedLatencyBlock(string rootNamespace, TimeSpan flushPeriod, List<int> percentiles)
    {
      var latencies = new Dictionary<string, List<int>>();
      var root = rootNamespace;
      var spinLock = new SpinLock();
      var outgoing = new BufferBlock<GraphiteLine>();
      var ns = String.IsNullOrEmpty(rootNamespace) ? "" : rootNamespace + ".";

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
            if (gotLock) {
              spinLock.Exit(false);
            }
          }
        });
      var intervalHandle = Utility.SetInterval(flushPeriod, () =>
        {
          if (latencies.Count == 0)
          {
            return;
          }
          var epoch = Utility.GetEpoch();
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
            if (gotLock) {
              spinLock.Exit(false);
            }
          }
          int percentileValue;
          foreach( var measurements in bucketOfLatencies )
          {
            outgoing.Post(new GraphiteLine(ns + measurements.Key + ".count", measurements.Value.Count));
            outgoing.Post(new GraphiteLine(ns + measurements.Key + ".min", measurements.Value.Min()));
            outgoing.Post(new GraphiteLine(ns + measurements.Key + ".max", measurements.Value.Max()));
            outgoing.Post(new GraphiteLine(ns + measurements.Key + ".mean", Convert.ToInt32(measurements.Value.Average())));
            outgoing.Post(new GraphiteLine(ns + measurements.Key + ".sum", measurements.Value.Sum()));
            // Now do percentiles
            foreach (var percentile in percentiles)
            {
              if (Percentile.TryCompute(measurements.Value, percentile, out percentileValue))
              {
                outgoing.Post(new GraphiteLine(ns + measurements.Key + ".p" + percentile, percentileValue));
              }
            }
          }
        });
      incoming.Completion.ContinueWith(p =>
        {
          // Stop the timer
          intervalHandle.Cancel();
          // Tell the upstream block that we're done
          outgoing.Complete();
        });
      return DataflowBlock.Encapsulate(incoming, outgoing);
    }
  }
}
