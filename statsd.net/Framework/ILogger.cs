using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Framework
{
  public interface ILogger
  {
    void Info(string message);
    void Error(string message);
    void Critical(string message);
  }

  public static class ILoggerExtensions
  {
    public static void Info(this ILogger logger, string message, params string[] formatParameters)
    {
      logger.Info(String.Format(message, formatParameters));
    }

    public static void Warning(this ILogger logger, string message, params string[] formatParameters)
    {
      logger.Warning(String.Format(message, formatParameters));
    }

    public static void Error(this ILogger logger, string message, params string[] formatParameters)
    {
      logger.Error(String.Format(message, formatParameters));
    }
  }
}
