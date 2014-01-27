using statsd.net.core;
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
using log4net;

namespace statsd.net.shared.Factories
{
  public static class MessageParserBlockFactory
  {
    public static TransformBlock<String, StatsdMessage> CreateMessageParserBlock(CancellationToken cancellationToken,
      ISystemMetricsService systemMetrics,
      ILog log)
    {
      var block = new TransformBlock<String, StatsdMessage>(
        (line) =>
        {
          systemMetrics.LogCount("parser.linesSeen");
          StatsdMessage message = StatsdMessageFactory.ParseMessage(line);
          if (message is InvalidMessage)
          {
            systemMetrics.LogCount("parser.badLinesSeen");
            log.Info("Bad message: " + ((InvalidMessage)message).Reason + Environment.NewLine + line);
          }
          return message;
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
