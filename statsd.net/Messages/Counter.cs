using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Messages
{
  public sealed class Counter : StatsdMessage
  {
    public int Value { get; set; }
    public float? SampleRate { get; set; }

    public Counter(string name, int value)
    {
      if (value < 0)
      {
        throw new ArgumentOutOfRangeException("value");
      }

      Name = name;
      Value = value;
      MessageType = MessageType.Counter;
    }

    public Counter(string name, int value, float sampleRate)
    {
      if (value < 0)
      {
        throw new ArgumentOutOfRangeException("value");
      }

      Name = name;
      Value = value;
      SampleRate = sampleRate;
      MessageType = MessageType.Counter;
    }

    public override string ToString()
    {
      if (SampleRate == null)
      {
        return String.Format("{0}:{1}|c", Name, Value);
      }
      else
      {
        return String.Format("{0}:{1}|c|@{2}", Name, Value, SampleRate.Value);
      }
    }
  }
}
