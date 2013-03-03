using statsd.net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.Listeners
{
  public class UdpStatsListener
  {
    private int _port;
    private CancellationToken _cancellationToken;

    public UdpStatsListener(int port, CancellationToken cancellationToken)
    {
      _port = port;
      _cancellationToken = cancellationToken;
    }

    public async void LinkTo( ITargetBlock<string> target)
    {
      var udpClient = new UdpClient(_port);
      while (true)
      {
        _cancellationToken.ThrowIfCancellationRequested();
        var data = await udpClient.ReceiveAsync();
        string rawPacket = Encoding.UTF8.GetString(data.Buffer);
        string[] lines = rawPacket.Remove('\r').Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
          target.Post(lines[index]);
        }
      }
    }
  }
}
