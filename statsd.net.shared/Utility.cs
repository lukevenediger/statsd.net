using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;

namespace statsd.net.shared
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

    public static IPAddress HostToIPv4Address(string host)
    {
      return Dns
        .GetHostAddresses(host)
        .First(p => p.AddressFamily == AddressFamily.InterNetwork);
    }

    public static ExecutionDataflowBlockOptions UnboundedExecution()
    {
      return new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = ExecutionDataflowBlockOptions.Unbounded };
    }
    
    public static ExecutionDataflowBlockOptions OneAtATimeExecution()
    {
      return new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = ExecutionDataflowBlockOptions.Unbounded };
    }

    public static TimeSpan ConvertToTimespan(string time)
    {
      string amount = String.Empty;
      foreach (var character in time)
      {
        if (Char.IsNumber(character))
        {
          amount += character;
        }
        else if (Char.IsLetter(character))
        {
          var value = Int32.Parse(amount);
          switch (character)
          {
            case 's':
              return new TimeSpan(0, 0, value);
            case 'm':
              return new TimeSpan(0, value, 0);
            case 'h':
              return new TimeSpan(value, 0, 0);
            case 'd':
              return new TimeSpan(value, 0, 0, 0);
          }
        }
      }
      // Default to seconds if there isn't a postfix
      return new TimeSpan(0, 0, Int32.Parse(amount));
    }

  }
}
