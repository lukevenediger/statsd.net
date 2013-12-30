using statsd.net.core.Messages;
using statsd.net.core.Structures;
using statsd.net.shared.Messages;
using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net_Tests.Infrastructure
{
  public class BucketOutputBlock : OutputBufferBlock<Bucket>
  {
    private GraphiteLineOutputBlock _targetBlock;

    public List<GraphiteLine> GraphiteLines
    {
      get { return _targetBlock.Items; }
    }

    public BucketOutputBlock()
    {
      _targetBlock = new GraphiteLineOutputBlock();
    }

    protected override void OnMessageOffered(Bucket message)
    {
      message.FeedTarget(_targetBlock);
    }

    public GraphiteLine GetGraphiteLine(int line)
    {
      return _targetBlock[line];
    }

    public int this[string key]
    {
      get { return _targetBlock[key]; }
    }
  }
}