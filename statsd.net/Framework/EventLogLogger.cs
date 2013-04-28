using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Framework
{
  internal class EventLogLogger : ILogger
  {
    public EventLogLogger()
    {
    }

    public void CheckEventSource()
    {
      if (!EventLog.SourceExists("statsd.net"))
      {
        EventLog.CreateEventSource("statsd.net", "statsd.net.logs");
      }
    }

    public void DiscardEventSource()
    {
      if (EventLog.SourceExists("statsd.net"))
      {
        EventLog.DeleteEventSource("statsd.net");
        EventLog.Delete("statsd.net.logs");
      }
    }

    public void Info(string message)
    {
      throw new NotImplementedException();
    }

    public void Error(string message)
    {
      throw new NotImplementedException();
    }

    public void Critical(string message)
    {
      throw new NotImplementedException();
    }
  }
}
