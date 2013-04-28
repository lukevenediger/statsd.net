using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;

namespace statsd.net
{
  public static class Utility
  {
    public static TimerHandle SetInterval(TimeSpan delay, Action callback)
    {
      var timer = new Timer(delay.TotalMilliseconds);
      timer.Elapsed += (sender, e) =>
        {
          timer.Stop();
          callback();
          timer.Start();
        };
      timer.AutoReset = false;
      timer.Start();
      return new TimerHandle(timer, callback);
    }

    public class TimerHandle
    {
      private Timer _timer;
      private Action _callback;

      public TimerHandle(Timer timer, Action callback)
      {
        _timer = timer;
        _callback = callback;
      }

      public void Cancel()
      {
        _timer.Stop();
      }

      public void RunOnce()
      {
        _callback();
      }
    }

    public static long GetEpoch ()
    {
      return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
    }

    public static long ToEpoch(this DateTime dateTime)
    {
      return (dateTime.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
    }

    public static IPAddress HostToIPv4Address(string host)
    {
      return Dns
        .GetHostAddresses(host)
        .First(p => p.AddressFamily == AddressFamily.InterNetwork);
    }

    public static void CompleteAndWait(this IDataflowBlock block)
    {
      block.Complete();
      block.Completion.Wait();
    }

    public static void WaitUntilAllItemsProcessed<T>(this ActionBlock<T> block, int sleepTimeMS = 100)
    {
      while (true)
      {
        if (block.InputCount == 0)
        {
          return;
        }
        System.Threading.Thread.Sleep(sleepTimeMS);
      }
    }
  }
}
