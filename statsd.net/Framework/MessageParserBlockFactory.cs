using statsd.net.Listeners;
using statsd.net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net.Services;

namespace statsd.net.Framework
{
  public static class MessageParserBlockFactory
  {
    public static TransformBlock<String, StatsdMessage> CreateMessageParserBlock(CancellationToken cancellationToken,
      ISystemMetricsService systemMetrics)
    {
      var block = new TransformBlock<String, StatsdMessage>(
        (line) =>
        {
          StatsdMessage message;
          if (StatsdMessageFactory.TryParseMessage(line, out message))
          {
            systemMetrics.ProcessedALine();
            return message;
          }
          else
          {
            systemMetrics.SawBadLine();
            return InvalidMessage.Instance;
          }
        },
        new ExecutionDataflowBlockOptions()
        {
          MaxDegreeOfParallelism = ExecutionDataflowBlockOptions.Unbounded,
          CancellationToken = cancellationToken
        });
      return block;
    }
  }
}
