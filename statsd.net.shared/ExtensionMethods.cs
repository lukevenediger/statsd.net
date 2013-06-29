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
}
