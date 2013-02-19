using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
          callback();
        };
      timer.AutoReset = true;
      timer.Start();
      return new TimerHandle(timer);
    }

    public class TimerHandle
    {
      private Timer _timer;

      public TimerHandle(Timer timer)
      {
        _timer = timer;
      }

      public void Cancel()
      {
        _timer.Stop();
      }
    }

    public static long GetEpoch ()
    {
      return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
    }
  }
}
