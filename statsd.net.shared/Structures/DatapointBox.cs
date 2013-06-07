using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Structures
{
  public class DatapointBox
  {
    private object _sync;
    private int _size;
    private int[] _points;
    private LinkedList<int> _pointsList;
    private bool _reachedLimit;
    private Random _random;

    public DatapointBox(int size, int? firstDataPoint = null)
    {
      _size = size;
      _sync = new object();
      _pointsList = new LinkedList<int>();
      _reachedLimit = false;
      _random = new Random();

      if (firstDataPoint.HasValue)
      {
        _pointsList.AddLast(firstDataPoint.Value);
      }
    }

    public virtual void Add(int dataPoint)
    {
      lock (_sync)
      {
        AddInternal(dataPoint);
      }
    }

    protected void AddInternal(int dataPoint)
    {
        if (_reachedLimit)
        {
          // We're sampling the incoming dataset, so replace a random point
          _points[_random.Next(_size)] = dataPoint;
        }
        else
        {
          _pointsList.AddLast(dataPoint);
          if (_pointsList.Count == _size)
          {
            // transfer the list to the array
            _points = _pointsList.ToArray();
            _reachedLimit = true;
            _pointsList.Clear();
            _pointsList = null;
          }
        }
    }

    public int[] ToArray()
    {
      lock (_sync)
      {
        return _reachedLimit ? _points : _pointsList.ToArray();
      }
    }
  }
}
