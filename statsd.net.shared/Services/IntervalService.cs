using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using statsd.net;
using System.Timers;
using System.Threading;
using System.Diagnostics;

namespace statsd.net.shared.Services
{
  public interface IIntervalService
  {
    event EventHandler<IntervalFiredEventArgs> Elapsed;
    void Start();
    void Cancel();
    void RunOnce();
  }

  [DebuggerDisplay("Fires every {_timer.Interval} milliseconds.")]
  public class IntervalService : IIntervalService
  {
    private System.Timers.Timer _timer;
    private ManualResetEvent _callbackComplete;

    public IntervalService(TimeSpan delay)
    {
      _callbackComplete = new ManualResetEvent(true);
      _timer = new System.Timers.Timer(delay.TotalMilliseconds);
      _timer.Elapsed += (sender, e) =>
        {
          _callbackComplete.Reset();
          FireEvent( e.SignalTime.ToEpoch() );
          _callbackComplete.Set();           
        };
      _timer.AutoReset = true;
    }

    public IntervalService(int delayInSeconds)
      :this (new TimeSpan(0,0,delayInSeconds))
    {
    }

    public void Start()
    {
      _timer.Start();
    }

    public void Cancel()
    {
      _timer.Stop();
      // Wait until the callback has finised executing
      _callbackComplete.WaitOne(new TimeSpan(0, 0, 30));
    }

    public void RunOnce()
    {
      FireEvent(DateTime.Now.ToEpoch());
    }

    private void FireEvent ( long epoch )
    {
      if ( Elapsed != null )
      {
        Elapsed( this, new IntervalFiredEventArgs( epoch ) );
      }
    }

    public event EventHandler<IntervalFiredEventArgs> Elapsed;
  }

  [DebuggerDisplay("{Epoch}")]
  public class IntervalFiredEventArgs : EventArgs
  {
    public long Epoch { get; private set; } 

    public IntervalFiredEventArgs(long epoch)
    {
      Epoch = epoch;
    }
  }
}
