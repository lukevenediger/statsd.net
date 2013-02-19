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
    private IPropagatorBlock<StatsdMessage, GraphiteLine[]> _block;
    private CancellationTokenSource _cancelTokenSource;
    private UdpClient _udpClient;
    
    public UdpStatsListener(int port, IPropagatorBlock<StatsdMessage, GraphiteLine[]> block)
    {
      _block = block;
      _cancelTokenSource = new CancellationTokenSource();
      _udpClient = new UdpClient(port);
    }

    public void Listen()
    {
      var token = _cancelTokenSource.Token;
      var remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
      var listenTask = Task.Factory.StartNew(() =>
        {
          token.ThrowIfCancellationRequested();
          while (true)
          {
            var data = _udpClient.Receive(ref remoteEndpoint);
            string rawPacket = Encoding.UTF8.GetString(data);
            string[] lines = rawPacket.Remove('\r').Split('\n');
            StatsdMessage message;
            for (int index = 0; index < lines.Length; index++)
            {
              message = StatsdMessageFactory.ParseMessage(lines[index]);
              _block.Post(message);
            }
          }
        },
        cancellationToken: _cancelTokenSource.Token);
    }
  }
}
