using log4net;
using statsd.net.shared;
using statsd.net.shared.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

public static class ExtensionMethods
{
    public static long ToEpoch(this DateTime dateTime)
    {
        return (dateTime.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
    }

    public static void CompleteAndWait(this IDataflowBlock block)
    {
        block.Complete();
        block.Completion.Wait();
    }

    public static void WaitUntilAllItemsProcessed<TIn, TOut>(this TransformBlock<TIn, TOut> block, int sleepTimeMS = 100)
    {
        WaitUntilPredicate(p => p.InputCount == 0, block, sleepTimeMS);
    }

    public static void WaitUntilAllItemsProcessed<T>(this ActionBlock<T> block, int sleepTimeMS = 100)
    {
        WaitUntilPredicate(p => p.InputCount == 0, block, sleepTimeMS);
    }

    private static void WaitUntilPredicate<T>(Predicate<T> predicate, T target, int sleepTimeMS)
    {
        while (true)
        {
            if (predicate(target))
            {
                return;
            }
            System.Threading.Thread.Sleep(sleepTimeMS);
        }
    }

    public static void PostManyTo<T>(this IEnumerable<T> items, ITargetBlock<T> target)
    {
        foreach (T item in items)
        {
            target.Post(item);
        }
    }

    public static bool HasOwnProperty(this object target, string propertyName)
    {
        var dict = target as IDictionary<string, object>;
        return dict.ContainsKey(propertyName);
    }

    public static void LogAndContinueWith(this Task task, ILog log, string name, Action action)
    {
        task.ContinueWith(_ =>
          {
              switch (task.Status)
              {
                  case TaskStatus.Faulted:
                      log.Error(String.Format("{0} has faulted. Error: {1}", name, task.Exception.InnerException.GetType().Name),
                        task.Exception.InnerException);
                      break;
                  case TaskStatus.Canceled:
                      log.Warn(String.Format("{0} has been canceled.", name));
                      break;
                  default:
                      log.Info(String.Format("{0} is {1}.", name, task.Status.ToString()));
                      break;
              }
              action();
          });
    }

    public static int ToInt(this XElement element, string attributeName)
    {
        return int.Parse(element.Attribute(attributeName).Value);
    }

    public static bool ToBoolean(this XElement element, string attributeName)
    {
        return Boolean.Parse(element.Attribute(attributeName).Value);
    }

    public static TimeSpan ToTimeSpan(this XElement element, string attributeName)
    {
        var value = element.Attribute(attributeName).Value;
        return Utility.ConvertToTimespan(value);
    }

    public static bool ToBoolean(this XElement element, string attributeName, bool defaultValue)
    {
        if (!element.Attributes().Any(p => p.Name == attributeName))
        {
            return defaultValue;
        }
        return Boolean.Parse(element.Attribute(attributeName).Value);
    }

    public static byte[] Compress(this byte[] data)
    {
        using (var output = new MemoryStream())
        {
            using (var zip = new GZipStream(output, CompressionMode.Compress))
            {
                zip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }

    public static byte[] Scramble(this byte[] data)
    {
        var rand = new Random();
        rand.NextBytes(data);
        return data;
    }

    public static byte[] Decompress(this byte[] data)
    {
        using (var output = new MemoryStream())
        {
            using (var input = new MemoryStream(data))
            {
                using (var unzip = new GZipStream(input, CompressionMode.Decompress))
                {
                    unzip.CopyTo(output);
                }
            }
            return output.ToArray();
        }
    }

    /// <summary>
    /// Get the week number from a DateTime.
    /// </summary>
    /// <remarks>
    /// Source: http://stackoverflow.com/a/11155102
    /// </remarks>
    public static int GetIso8601WeekOfYear(this DateTime time)
    {
        // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
        // be the same week# as whatever Thursday, Friday or Saturday are,
        // and we always get those right
        DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            time = time.AddDays(3);
        }

        // Return the week of our adjusted day
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}