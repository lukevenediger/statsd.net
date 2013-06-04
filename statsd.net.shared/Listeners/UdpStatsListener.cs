using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Listeners
{
  public class UdpStatsListener : IListener
  {
    private int _port;
    private CancellationToken _cancellationToken;
    private ISystemMetricsService _systemMetrics;

    public UdpStatsListener(int port, ISystemMetricsService systemMetrics)
    {
      _port = port;
      _systemMetrics = systemMetrics;
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
              _systemMetrics.Log("listeners.udp.bytes", data.Length);
              string rawPacket = Encoding.UTF8.GetString(data);
              string[] lines = rawPacket.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
              for (int index = 0; index < lines.Length; index++)
              {
                target.Post(lines[index]);
              }
              _systemMetrics.Log("listeners.udp.lines", lines.Length);
            }
          }
          catch (ObjectDisposedException) { /* Eat it, socket was closed */ }
        },
        cancellationToken);
    }
  }
}
