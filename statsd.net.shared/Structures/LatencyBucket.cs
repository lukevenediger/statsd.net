using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Structures
{
  public class LatencyBucket : DatapointBox
  {
    private object _sync;

    public int Min { get; private set; }
    public int Max { get; private set; }
    public int Count { get; private set; }
    public int Sum { get; private set; }
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
      if (firstDataPoint.HasValue) AddInternal(firstDataPoint.Value);
    }

    public override void Add(int dataPoint)
    {
      lock (_sync)
      {
        Sum += dataPoint;
        Count++;
        if (Min > dataPoint) Min = dataPoint;
        if (Max < dataPoint) Max = dataPoint;

        base.AddInternal(dataPoint);
      }
    }
  }
}
