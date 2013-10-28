using statsd.net.shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Structures
{
  public class RawBucket : Bucket
  {
    private Raw[] _rawLines;

    public RawBucket(Raw[] rawLines, long epoch)
      : base(BucketType.Raw, epoch)
    {
      _rawLines = rawLines;
    }

    public override void FeedTarget(ITargetBlock<GraphiteLine> target)
    {
      foreach (var line in _rawLines)
      {
        for (int index = 0; index < _rawLines.Length; index++)
        {
          target.Post(new GraphiteLine(line.Name, line.Value, line.Timestamp ?? Epoch));
        }
      }
    }
  }
}
