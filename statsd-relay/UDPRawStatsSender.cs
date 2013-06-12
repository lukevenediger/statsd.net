using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace statsd.relay
{
  public class UDPRawStatsSender
  {
    private ISystemMetricsService _systemMetrics;
    private UdpClient _client;

    public UDPRawStatsSender(string host, int port, ISystemMetricsService systemMetrics)
    {
      var ipv4Address = Dns.GetHostAddresses(host).First(p => p.AddressFamily == AddressFamily.InterNetwork);
      _client = new UdpClient();
      _client.Connect(ipv4Address, port);
      _systemMetrics = systemMetrics;
    }

    public void Send(StatsdMessage[] lines)
    {
      string[] rawLines = lines.Select(p => p.ToString()).ToArray();
      byte[] payload = Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, rawLines));
      _client.Send(payload, payload.Length);
      _systemMetrics.LogCount("sent.bytes", payload.Length);
      _systemMetrics.LogCount("sent.lines", payload.Length); 
    }
  }
}
