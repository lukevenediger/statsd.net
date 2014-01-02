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
  public class CounterBucket : Bucket<int>
  {
    public CounterBucket(KeyValuePair<string, int>[] counts, long epoch, string rootNamespace = "")
      : base(BucketType.Count, counts, epoch, rootNamespace)
    {
    }

    public override GraphiteLine[] ToLines()
    {
      var lines = new List<GraphiteLine>();
      foreach (var count in Items)
      {
        lines.Add(new GraphiteLine(RootNamespace + count.Key, count.Value, Epoch));
      }
      return lines.ToArray();
    }

    public override void FeedTarget(ITargetBlock<GraphiteLine> target)
    {
      foreach (var count in Items)
      {
        target.Post(new GraphiteLine(RootNamespace + count.Key, count.Value, Epoch));
      }
    }

    public override string ToString()
    {
      var lines = new List<string>();
      foreach (var count in Items)
      {
        lines.Add(new GraphiteLine(RootNamespace + count.Key, count.Value, Epoch).ToString()); 
      }
      return String.Join(Environment.NewLine, lines.ToArray());
    }
  }
}
