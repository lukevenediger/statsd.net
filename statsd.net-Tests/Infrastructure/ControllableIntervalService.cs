using statsd.net.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using statsd.net;
using System.Diagnostics;

namespace statsd.net_Tests.Infrastructure
{
  public class ControllableIntervalService : IIntervalService
  {
    public bool StartCalled { get; private set; }
    public bool CancelCalled { get; private set; }
    public bool RunOnceCalled { get; private set; }
    public DateTime RunOnceDateTime { get; set; }
    
    public void Start()
    {
      StartCalled = true;
    }

    public void Cancel()
    {
      CancelCalled = true;
    }

    public void RunOnce()
    {
      FireEvent( RunOnceDateTime.ToEpoch() );
      RunOnceCalled = true;
    }

    public DateTime Pulse()
    {
      return Pulse(DateTime.Now);
    }

    public DateTime Pulse(DateTime? pulseDateTime = null)
    {
      pulseDateTime = pulseDateTime ?? DateTime.Now;
      FireEvent( RunOnceDateTime.ToEpoch() );
      return pulseDateTime.Value;
    }

    private void FireEvent ( long epoch )
    {
      if ( Elapsed != null )
      {
        Elapsed( this, new IntervalFiredEventArgs( epoch ) );
      }
    }

    public void Reset()
    {
      CancelCalled = false;
      RunOnceCalled = false;
      StartCalled = false;
      RunOnceDateTime = DateTime.MinValue;
    }

    public event EventHandler<IntervalFiredEventArgs> Elapsed;
  }
}
