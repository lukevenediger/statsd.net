using statsd.net.Messages;
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
    public static TransformBlock<String, StatsdMessage> CreateMessageParserBlock(CancellationToken cancellationToken)
    {
      var block = new TransformBlock<String, StatsdMessage>(
        (line) =>
        {
          return StatsdMessageFactory.ParseMessage(line);
        },
        new ExecutionDataflowBlockOptions()
        {
          //MaxDegreeOfParallelism = ExecutionDataflowBlockOptions.Unbounded,
          CancellationToken = cancellationToken
        });
      return block;    }
  }
}
