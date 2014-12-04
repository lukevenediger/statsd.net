using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace statsd.net.shared.Structures
{
  public class LatencyDatapointBox : DatapointBox
  {
    private object _sync;

    public double Min { get; private set; }
    public double Max { get; private set; }
    private int _count;
    public int Count { get { return _count; } }
    private double _sum;
    public double Sum { get { return _sum; } }
    public double Mean
    {
      get
      {
        return base.ToArray().Average();
      }
    }
    private double _sumSquares;
    public double SumSquares { get { return _sumSquares; } }

    public LatencyDatapointBox(int maxItems = 1000, double? firstDataPoint = null)
      : base(maxItems)
    {
      _sync = new Object();
      Min = -1;
      Max = -1;
      _count = 0;
      if (firstDataPoint.HasValue) Add(firstDataPoint.Value);
    }

    public override void Add(double dataPoint)
    {
      AtomicAdd(ref _sum, dataPoint);
      Interlocked.Increment(ref _count);
      AtomicAdd(ref _sumSquares, dataPoint * dataPoint);
      lock (_sync)
      {
        if (Min == -1 || dataPoint < Min) Min = dataPoint;
        if (Max == -1 || dataPoint > Max) Max = dataPoint;

        base.AddInternal(dataPoint);
      }
    }

    public double AtomicAdd(ref double location1, double value)
    {
      double newCurrentValue = 0;
      while (true)
      {
        double currentValue = newCurrentValue;
        double newValue = currentValue + value;
        newCurrentValue = Interlocked.CompareExchange(ref location1, newValue, currentValue);
        if (newCurrentValue == currentValue)
          return newValue;
       }
     }
  }
}
