using statsd.net.Listeners;
using statsd.net.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Topshelf;

namespace statsd.net
{
  public class ServiceWrapper : ServiceControl
  {
    CancellationTokenSource _tokenSource;
    public ServiceWrapper()
    {
      _tokenSource = new CancellationTokenSource();
    }

    public bool Start(HostControl hostControl)
    {
      var udpListener = new UdpStatsListener(12000, _tokenSource.Token);
      var messageParser = MessageParserBlockFactory.CreateMessageParserBlock(_tokenSource.Token);
      var dataAggregator = TimedDataBlockFactory.CreateTimedBlock(new TimeSpan(0,0,10));
      udpListener.LinkTo(messageParser);
      messageParser.LinkTo(dataAggregator, new DataflowLinkOptions());
      return true;
    }

    public bool Stop(HostControl hostControl)
    {
      return false;
    }
  }
}
