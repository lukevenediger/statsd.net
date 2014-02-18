using statsd.net.core.Messages;
using statsd.net.core.Structures;
using statsd.net.shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Structures
{
  public class GaugesBucket : Bucket
  {
    public KeyValuePair<string, int>[] Gauges { get; set; }

    public GaugesBucket(KeyValuePair<string, int>[] gauges, long epoch, string rootNamespace = "")
      : base(BucketType.Gauge, epoch, rootNamespace)
    {
      Gauges = gauges;
    }

    public override GraphiteLine[] ToLines()
    {
      var lines = new List<GraphiteLine>();
      foreach (var gauge in Gauges)
      {
        lines.Add(new GraphiteLine(RootNamespace + gauge.Key, gauge.Value, Epoch));
      }
      return lines.ToArray();
    }

    public override void FeedTarget(ITargetBlock<GraphiteLine> target)
    {
      foreach (var gauge in Gauges)
      {
        target.Post(new GraphiteLine(RootNamespace + gauge.Key, gauge.Value, Epoch));
      }
    }

    public override string ToString()
    {
      var lines = new List<string>();
      foreach (var count in Gauges)
      {
        lines.Add(new GraphiteLine(RootNamespace + count.Key, count.Value, Epoch).ToString()); 
      }
      return String.Join(Environment.NewLine, lines.ToArray());
    }
  }
}
