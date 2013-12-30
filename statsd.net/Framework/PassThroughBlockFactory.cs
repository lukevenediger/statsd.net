using log4net;
using statsd.net.core.Structures;
using statsd.net.shared;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net.shared.Structures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.Framework
{
  /// <summary>
  /// Batches and sends messages through
  /// </summary>
  public class PassThroughBlockFactory
  {
    public static ActionBlock<StatsdMessage> CreateBlock(ITargetBlock<Bucket> target,
      IIntervalService intervalService)
    {
      var rawLines = new ConcurrentStack<Raw>();
      var incoming = new ActionBlock<StatsdMessage>(p =>
        {
          rawLines.Push(p as Raw);
        },
        Utility.UnboundedExecution());

      intervalService.Elapsed += (sender, e) =>
        {
          if (rawLines.Count == 0)
          {
            return;
          }
          var lines = rawLines.ToArray();
          rawLines.Clear();
          var bucket = new RawBucket(lines, e.Epoch);
          target.Post(bucket);
        };
      return incoming;
    }
  }
}
