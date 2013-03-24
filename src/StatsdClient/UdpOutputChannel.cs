using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
      // Convert to ipv4 address
      var ipv4Address = Dns.GetHostAddresses(host).First(p => p.AddressFamily == AddressFamily.InterNetwork);
      _udpClient = new UdpClient();
      _udpClient.Connect(ipv4Address, port);
    }

    public void Send(string line)
    {
      byte[] payload = Encoding.UTF8.GetBytes(line);
      _udpClient.Send(payload, payload.Length);
    }
  }
}
