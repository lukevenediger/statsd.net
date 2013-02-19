using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Messages
{
  public class GraphiteLine
  {
    private string _name;
    private int _quantity;
    private long _epoc;

    public GraphiteLine(string name, int quantity)
    {
      _name = name;
      _quantity = quantity;
      _epoc = Utility.GetEpoch();
    }

    public GraphiteLine(string name, int quantity, long epoc)
    {
      _name = name;
      _quantity = quantity;
      _epoc = epoc;
    }

    public override string ToString()
    {
      return _name + " " + _quantity + " " + _epoc;
    }
  }
}
