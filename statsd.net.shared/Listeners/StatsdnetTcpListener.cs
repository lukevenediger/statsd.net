using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net.shared.Structures;
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
  public class StatsdnetTcpListener : IListener
  {
    private const int READ_TIMEOUT = 5000; /* 5 seconds */
    private static string[] SPACE_SPLITTER = new String[] { " " };
    private static string[] NEWLINE_SPLITTER = new String[] { Environment.NewLine };

    private ITargetBlock<string> _target;
    private CancellationToken _token;
    private ISystemMetricsService _systemMetrics;
    private TcpListener _tcpListener;
    private int _activeConnections;
    private ActionBlock<DecoderBlockPacket> _decoderBlock;

    public bool IsListening { get; private set; }

    public StatsdnetTcpListener(int port, ISystemMetricsService systemMetrics)
    {
      _systemMetrics = systemMetrics;
      IsListening = false;
      _activeConnections = 0;
      _tcpListener = new TcpListener(IPAddress.Any, port);
      _decoderBlock = new ActionBlock<DecoderBlockPacket>((data) => { DecodePacketAndForward(data); },
        Utility.UnboundedExecution());
    }

    public async void LinkTo(ITargetBlock<string> target, CancellationToken token)
    {
      _target = target;
      _token = token;
      await Listen();
    }

    private async Task Listen()
    {
      _tcpListener.Start();
      IsListening = true;
      while(!_token.IsCancellationRequested)
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
        _systemMetrics.LogGauge("listeners.statsdnet.activeConnections", _activeConnections);
        _systemMetrics.LogCount("listeners.statsdnet.connection.open");
        using (BinaryReader reader = new BinaryReader(tcpClient.GetStream()))
        {
          while (true)
          {
            if (reader.PeekChar() == 0)
            {
              // close the socket
              return;
            }
            // Get the length
            var packetLength = reader.ReadInt32();
            // Is it compressed?
            var isCompressed = reader.ReadBoolean();
            // Now get the packet
            var packet = reader.ReadBytes(packetLength);
            // Decode
            _decoderBlock.Post(new DecoderBlockPacket(packet, isCompressed));
          }
        }
      }
      catch (SocketException se)
      {
        // oops, we're done  
        _systemMetrics.LogCount("listeners.statsdnet.error.SocketException." + se.SocketErrorCode.ToString());
      }
      catch (IOException io)
      {
        // Not much we can do here.
        _systemMetrics.LogCount("listeners.statsdnet.error.IOException");
      }
      catch (Exception ex)
      {
        var a = 1;
      }
      finally
      {
        try
        {
          tcpClient.Close();
        }
        catch
        {
          // Do nothing but log that this happened
          _systemMetrics.LogCount("listeners.statsdnet.error.closeThrewException");
        }

        _systemMetrics.LogCount("listeners.statsdnet.connection.closed");
        Interlocked.Decrement(ref _activeConnections);
        _systemMetrics.LogGauge("listeners.statsdnet.activeConnections", _activeConnections);
      }
    }

    private void DecodePacketAndForward(DecoderBlockPacket packet)
    {
      try
      {
        byte[] rawData;
        if (packet.isCompressed)
        {
          _systemMetrics.LogCount("listeners.statsdnet.bytes.gzip", packet.data.Length);
          rawData = packet.data.Decompress();
        }
        else
        {
          rawData = packet.data;
        }

        _systemMetrics.LogCount("listeners.statsdnet.bytes.raw", rawData.Length);
        var lines = Encoding.UTF8.GetString(rawData).Split(
          NEWLINE_SPLITTER,
          StringSplitOptions.RemoveEmptyEntries
        );
        foreach(var line in lines)
        {
          // Format this as raw and send it on.
          var parts = line.Split(SPACE_SPLITTER, StringSplitOptions.RemoveEmptyEntries);
          _target.Post(parts[0] + ":" + parts[1] + "|r|" + parts[2]);
        }
        _systemMetrics.LogCount("listeners.statsdnet.lines", lines.Length);
      }
      catch (Exception ex)
      {
        _systemMetrics.LogCount("listeners.statsdnet.decodingError." + ex.GetType().Name);
      }
    }

  }
}
