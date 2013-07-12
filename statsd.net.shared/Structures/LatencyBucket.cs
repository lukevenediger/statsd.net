using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace statsd.net.shared.Structures
{
  public class LatencyBucket : DatapointBox
  {
    private object _sync;

    public int Min { get; private set; }
    public int Max { get; private set; }
    private int _count;
    public int Count { get { return _count; } }
    private int _sum;
    public int Sum { get { return _sum; } }
    public int Mean
    {
      get
      {
        return Convert.ToInt32(base.ToArray().Average());
      }
    }

    public LatencyBucket(int maxItems = 1000, int? firstDataPoint = null)
      : base(maxItems)
    {
      _sync = new Object();
      Min = -1;
      Max = -1;
      _count = 0;
      if (firstDataPoint.HasValue) Add(firstDataPoint.Value);
    }

    public override void Add(int dataPoint)
    {
      Interlocked.Add(ref _sum, dataPoint);
      Interlocked.Increment(ref _count);
      lock (_sync)
      {
        if (Min == -1 || dataPoint < Min) Min = dataPoint;
        if (Max == -1 || dataPoint > Max) Max = dataPoint;

        base.AddInternal(dataPoint);
      }
    }
  }
}
