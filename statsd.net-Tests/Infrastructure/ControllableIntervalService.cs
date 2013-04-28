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
    private Action<long> _handler;

    public bool StartCalled { get; private set; }
    public bool CancelCalled { get; private set; }
    public bool RunOnceCalled { get; private set; }
    public DateTime RunOnceDateTime { get; set; }
    
    public Action<long> Elapsed
    {
      set { _handler = value; }
    }

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
      _handler(RunOnceDateTime.ToEpoch());
      RunOnceCalled = true;
    }

    public DateTime Pulse()
    {
      return Pulse(DateTime.Now);
    }

    public DateTime Pulse(DateTime? pulseDateTime = null)
    {
      pulseDateTime = pulseDateTime ?? DateTime.Now;
      _handler(pulseDateTime.Value.ToEpoch());
      return pulseDateTime.Value;
    }

    public void Reset()
    {
      CancelCalled = false;
      RunOnceCalled = false;
      StartCalled = false;
      RunOnceDateTime = DateTime.MinValue;
    }
  }
}
