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
  public class UdpStatsListener : IListener
  {
    private int _port;
    private CancellationToken _cancellationToken;

    public UdpStatsListener(int port)
    {
      _port = port;
    }

    public void LinkTo(ITargetBlock<string> target, CancellationToken cancellationToken)
    {
      _cancellationToken = cancellationToken;
      Task.Factory.StartNew(() =>
        {
          try
          {
            var endpoint = new IPEndPoint(IPAddress.Any, _port);
            var udpClient = new UdpClient(endpoint);
            while (true)
            {
              if (_cancellationToken.IsCancellationRequested)
              {
                return;
              }
              byte[] data = udpClient.Receive(ref endpoint);
              string rawPacket = Encoding.UTF8.GetString(data);
              string[] lines = rawPacket.Replace("\r", "").Split('\n');
              for (int index = 0; index < lines.Length; index++)
              {
                target.Post(lines[index]);
              }
            }
          }
          catch (ObjectDisposedException) { /* Eat it, socket was closed */ }
        },
        cancellationToken);
    }
  }
}
