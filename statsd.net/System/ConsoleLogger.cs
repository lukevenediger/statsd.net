using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.System
{
  internal class ConsoleLogger : ILogger
  {
    public void Info(string message)
    {
      Console.WriteLine("I: " + message);
    }

    public void Error(string message)
    {
      Console.WriteLine("E: " + message);
    }

    public void Critical(string message)
    {
      Console.WriteLine("C: " + message);
    }
  }
}
