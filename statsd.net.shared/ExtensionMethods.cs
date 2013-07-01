using log4net;
using statsd.net.shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

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
}
