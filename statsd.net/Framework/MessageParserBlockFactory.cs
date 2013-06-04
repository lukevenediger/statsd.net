using statsd.net.shared.Listeners;
using statsd.net.shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net.shared.Services;

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
          systemMetrics.Log("parser.linesSeen");
          if (StatsdMessageFactory.TryParseMessage(line, out message))
          {
            return message;
          }
          else
          {
            systemMetrics.Log("parser.badLinesSeen");
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
