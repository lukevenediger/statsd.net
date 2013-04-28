using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using statsd.net;
using System.Timers;
using System.Threading;

namespace statsd.net.Services
{
  public interface IIntervalService
  {
    Action<long> Elapsed { set; }
    void Start();
    void Cancel();
    void RunOnce();
  }

  public class IntervalService : IIntervalService
  {
    private Action<long> _handler;
    private System.Timers.Timer _timer;
    private ManualResetEvent _callbackComplete;

    public IntervalService(TimeSpan delay)
    {
      _callbackComplete = new ManualResetEvent(true);
      _timer = new System.Timers.Timer(delay.TotalMilliseconds);
      _timer.Elapsed += (sender, e) =>
        {
          _callbackComplete.Reset();
          _timer.Stop();
          _handler(e.SignalTime.ToEpoch());
          _timer.Start();
          _callbackComplete.Set();           
        };
      _timer.AutoReset = false;
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
      _handler(DateTime.Now.ToEpoch());
    }

    public Action<long> Elapsed
    {
      set { _handler = value; }
    }
  }
}
