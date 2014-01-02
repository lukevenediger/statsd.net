using System;
using System.Linq;

namespace statsd.net.core.Messages
{
  public class GraphiteLine
  {
    private readonly int _quantity;
    private readonly long _epoc;

    public string Name { get; private set; }
    public int Quantity { get { return _quantity; } }
    public long Epoc { get { return _epoc; } }

    public GraphiteLine(string name, 
      int quantity, 
      long? epoc = null)
    {
      Name = name;
      _quantity = quantity;
      _epoc = epoc ?? GetEpoch();
    }

    public static long GetEpoch()
    {
      return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
    }

    public override string ToString()
    {
      return Name + " " + _quantity + " " + _epoc;
    }

    public static GraphiteLine Clone(GraphiteLine line)
    {
      return new GraphiteLine(line.Name, line._quantity, line._epoc);
    }

    public static GraphiteLine[] CloneMany(GraphiteLine[] line)
    {
      return line.Select(p => GraphiteLine.Clone(p)).ToArray();
    }

    public override int GetHashCode()
    {
      return Name.GetHashCode() ^ _quantity.GetHashCode() ^ _epoc.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (obj == null)
      {
        return false;
      }
      if (!(obj is GraphiteLine))
      {
        return false;
      }
      return Equals(obj as GraphiteLine);
    }

    public bool Equals(GraphiteLine line)
    {
      return line.Name == this.Name &&
        line._quantity == this._quantity &&
        line._epoc == this._epoc;
    }
  }
}
