using statsd.net.Listeners;
using statsd.net.Messages;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.System
{
  public static class MessageParserBlockFactory
  {
    public static TransformBlock<String, StatsdMessage> CreateMessageParserBlock(CancellationToken cancellationToken,
      SystemEventListener systemEvents)
    {
      var block = new TransformBlock<String, StatsdMessage>(
        (line) =>
        {
          StatsdMessage message;
          if (StatsdMessageFactory.TryParseMessage(line, out message))
          {
            return message;
          }
          else
          {
            systemEvents.Send(_.count.statsdnet.badlines + 1);
            return InvalidMessage.Instance;
          }
        },
        new ExecutionDataflowBlockOptions()
        {
          MaxDegreeOfParallelism = ExecutionDataflowBlockOptions.Unbounded,
          CancellationToken = cancellationToken
        });
      return block;    }
  }
}
