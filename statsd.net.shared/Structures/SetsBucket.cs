using statsd.net.shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Structures
{
  public class SetsBucket : Bucket
  {
    public List<KeyValuePair<string, List<KeyValuePair<int, bool>>>> Sets { get; private set; }

    public SetsBucket(List<KeyValuePair<string, List<KeyValuePair<int, bool>>>> sets, 
      long epoch, 
      string rootNamespace = "")
      : base(BucketType.Set, epoch, rootNamespace)
    {
      Sets = sets;
    }

    public override GraphiteLine[] ToLines()
    {
      var lines = new List<GraphiteLine>();
      foreach (var set in Sets)
      {
        foreach (var item in set.Value)
        {
          lines.Add(new GraphiteLine(RootNamespace + set.Key, 1, Epoch));
        }
      }
      return lines.ToArray();
    }

    public override void FeedTarget(ITargetBlock<GraphiteLine> target)
    {
      foreach (var set in Sets)
      {
        foreach (var item in set.Value)
        {
          target.Post(new GraphiteLine(RootNamespace + set.Key, 1, Epoch));
        }
      }
    }
  }
}
