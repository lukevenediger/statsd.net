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
  public class RawBucket : Bucket
  {
    private Raw[] _rawLines;

    public RawBucket(Raw[] rawLines, long epoch)
      : base(BucketType.Raw, epoch)
    {
      _rawLines = rawLines;
    }

    public override GraphiteLine[] ToLines()
    {
      var lines = new List<GraphiteLine>();
      foreach (var line in _rawLines)
      {
        for (int index = 0; index < _rawLines.Length; index++)
        {
          lines.Add(new GraphiteLine(line.Name, line.Value, line.Timestamp ?? Epoch));
        }
      }
      return lines.ToArray();
    }

    public override void FeedTarget(ITargetBlock<GraphiteLine> target)
    {
      foreach (var line in _rawLines)
      {
        target.Post(new GraphiteLine(line.Name, line.Value, line.Timestamp ?? Epoch));
      }
    }

    public override string ToString()
    {
      var lines = _rawLines.Select(line =>
        new GraphiteLine(line.Name, line.Value, line.Timestamp ?? Epoch).ToString())
        .ToArray();
      return String.Join(Environment.NewLine, lines);
    }
  }
}
