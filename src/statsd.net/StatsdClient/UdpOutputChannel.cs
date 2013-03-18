using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace StatsdClient
{
  internal sealed class UdpOutputChannel : IOutputChannel
  {
    private UdpClient _udpClient;

    public UdpOutputChannel(string host, int port)
    {
      _udpClient = new UdpClient(host, port);
    }

    public void Send(string line)
    {
      byte[] payload = Encoding.UTF8.GetBytes(line);
      _udpClient.Send(payload, payload.Length);
    }
  }
}
