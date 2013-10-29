using statsd.net.shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Listeners
{
  public class TcpStatsListener : IListener
  {
    private ITargetBlock<string> _target;
    private CancellationToken _token;
    private ISystemMetricsService _systemMetrics;
    private TcpListener _tcpListener;
    private int _activeConnections;
    
    public bool IsListening { get; private set; }

    public TcpStatsListener(int port, ISystemMetricsService systemMetrics)
    {
      _systemMetrics = systemMetrics;
      IsListening = false;
      _activeConnections = 0;
      _tcpListener = new TcpListener(IPAddress.Any, port);
    }

    public async void LinkTo(ITargetBlock<string> target, CancellationToken token)
    {
      _target = target;
      _token = token;
      _tcpListener.Start();
      IsListening = true;
      await Listen();
    }

    public async Task Listen()
    {
      while (!_token.IsCancellationRequested)
      {
        var tcpClient = await _tcpListener.AcceptTcpClientAsync();
        ProcessIncomingConnection(tcpClient);
      }
    }

    private void ProcessIncomingConnection(TcpClient tcpClient)
    {
      try
      {
        Interlocked.Increment(ref _activeConnections);
        _systemMetrics.LogGauge("tcp.activeConnections", _activeConnections);
        _systemMetrics.LogCount("tcp.connection.open");
        using (var networkStream = tcpClient.GetStream())
        {
          var buffer = new byte[4096];
          while (!_token.IsCancellationRequested)
          {
            var byteCount = networkStream.Read(buffer, 0, buffer.Length);
            if ( byteCount == 0 )
            {
              return;
            }
            _systemMetrics.LogCount("tcp.bytes", byteCount);
            var lines = Encoding.UTF8.GetString(buffer, 0, byteCount).Replace("\r", "").Split('\n');
            // Post what we have
            _systemMetrics.LogCount("tcp.lines", lines.Length);
            lines.Where(p => !String.IsNullOrEmpty(p)).PostManyTo(_target);
            // Two blank lines means end the connection
            if (lines.Length >= 2 && lines[lines.Length - 2] == "" && lines[lines.Length - 1] == "")
            {
              return;
            }
          }
        }
      }
      catch (SocketException)
      {
        // oops, we're done  
      }
      catch (IOException)
      {
        // Not much we can do here.
      }
      finally
      {
        tcpClient.Close();
        _systemMetrics.LogCount("tcp.connection.closed");
        Interlocked.Increment(ref _activeConnections);
        _systemMetrics.LogGauge("tcp.activeConnections", _activeConnections);
      }
    }
  }
}
