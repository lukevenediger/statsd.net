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

    public override void FeedTarget(ITargetBlock<GraphiteLine> target)
    {
      int percentileValue;
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

    public bool TryComputePercentile(KeyValuePair<string, DatapointBox> pair, out int percentileValue)
    {
      return PercentileCalculator.TryCompute(
        pair.Value.ToArray().ToList(),
        Percentile,
        out percentileValue);
    }

    public override string ToString ()
    {
      var graphiteLines = new List<string>();
      int percentileValue;
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
