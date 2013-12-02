using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace statsd.net.shared.Structures
{
  public class DecoderBlockPacket
  {
    public byte[] data;
    public bool isCompressed;

    public DecoderBlockPacket(byte[] data, bool isCompressed)
    {
      this.data = data;
      this.isCompressed = isCompressed;
    }
  }
}
