using statsd.net.core.Messages;
using statsd.net.core.Structures;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Structures
{
  public class PercentileBucket : Bucket
  {
    public KeyValuePair<string, DatapointBox>[] Timings { get; private set; }
    public int Percentile { get; private set; }
    public string PercentileName { get; private set; }

    public PercentileBucket(KeyValuePair<string, DatapointBox>[] timings,
      long epoch,
      string rootNamespace,
      string percentileName,
      int percentile)
      : base(BucketType.Percentile, epoch, rootNamespace)
    {
      Timings = timings;
      PercentileName = percentileName;
      Percentile = percentile;
    }

    public override GraphiteLine[] ToLines()
    {
      var lines = new List<GraphiteLine>();
      double percentileValue;
      foreach (var measurements in Timings)
      {
        if (TryComputePercentile(measurements, out percentileValue))
        {
          lines.Add(
            new GraphiteLine(
              RootNamespace + measurements.Key + PercentileName, 
              percentileValue, 
              Epoch)
          );
        }
      }
      return lines.ToArray();
    }

    public override void FeedTarget(ITargetBlock<GraphiteLine> target)
    {
      double percentileValue;
      foreach (var measurements in Timings)
      {
        if (TryComputePercentile(measurements, out percentileValue))
        {
          target.Post(
            new GraphiteLine(
              RootNamespace + measurements.Key + PercentileName, 
              percentileValue, 
              Epoch));
        }
      }
    }

    public bool TryComputePercentile(KeyValuePair<string, DatapointBox> pair, out double percentileValue)
    {
      return PercentileCalculator.TryCompute(
        pair.Value.ToArray().ToList(),
        Percentile,
        out percentileValue);
    }

    public override string ToString ()
    {
      var graphiteLines = new List<string>();
      double percentileValue;
      foreach (var measurements in Timings)
      {
        if (TryComputePercentile(measurements, out percentileValue))
        {
          graphiteLines.Add(
            new GraphiteLine(
              RootNamespace + measurements.Key + PercentileName,
              percentileValue,
              Epoch ).ToString()
              );
        }
      }
      return String.Join(Environment.NewLine, graphiteLines.ToArray());
    }
  }
}
