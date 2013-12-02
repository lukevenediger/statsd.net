using statsd.net.shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Backends.Statsdnet
{
  public class StatsdnetForwardingClient
  {
    // Lots of opinions on what the correct value is: http://webmasters.stackexchange.com/questions/31750/what-is-recommended-minimum-object-size-for-gzip-performance-benefits
    private const int COMPRESSION_SIZE_THRESHOLD = 350;
    private TcpClient _client;
    private BinaryWriter _writer;
    private string _host;
    private int _port;
    private int _numRetries;
    private ISystemMetricsService _systemMetrics;

    public StatsdnetForwardingClient(string host, int port, ISystemMetricsService systemMetrics, int numRetries = 3)
    {
      _host = host;
      _port = port;
      _systemMetrics = systemMetrics;
      _numRetries = numRetries;
      _client = new TcpClient();
    }

    public bool Send (byte[] data)
    {
      return Send(data, _numRetries);
    }

    private bool Send (byte[] data, int retryAttemptsLeft)
    {
      /** Statsd.net packet format consists of
       * byte 0: 32-bit integer length
       * byte 33: boolean for whether the data is compressed or not
       * byte 34-: the packet
       */

      if (data.Length == 0)
      {
        return true;
      }

      Func<bool> handleRetry = () =>
        {
          _systemMetrics.LogCount("backends.statsdnet.sendFailed");
          if (retryAttemptsLeft > 0)
          {
            _systemMetrics.LogCount("backends.statsdnet.retrySend");
            return Send(data, --retryAttemptsLeft);
          }
          else
          {
            _systemMetrics.LogCount("backends.statsdnet.retrySendFailed");
            return false;
          }
        };

      try
      {
        if (!_client.Connected)
        {
          try
          {
            _client.Close();
          }
          catch (Exception)
          {
            // eat it
          }
          _client = new TcpClient();
          _client.Connect(_host, _port);
          _writer = new BinaryWriter(_client.GetStream());
        }
        if (data.Length < COMPRESSION_SIZE_THRESHOLD)
        {
          _writer.Write(data.Length);
          _writer.Write(false);
          _writer.Write(data);
          _systemMetrics.LogCount("backends.statsdnet.bytes.raw", data.Length);
          _systemMetrics.LogCount("backends.statsdnet.bytes.compressed", data.Length);
        }
        else
        {
          var compressedData = data.Compress();
          _writer.Write(compressedData.Length);
          _writer.Write(true);
          _writer.Write(compressedData);
          _systemMetrics.LogCount("backends.statsdnet.bytes.raw", data.Length);
          _systemMetrics.LogCount("backends.statsdnet.bytes.compressed", compressedData.Length);
        }
        return true;
      }
      catch (SocketException se)
      {
        _systemMetrics.LogCount("backends.statsdnet.error.SocketException." + se.SocketErrorCode.ToString());
        return handleRetry();
      }
      catch (IOException)
      {
        _systemMetrics.LogCount("backends.statsdnet.error.IOException");
        return handleRetry();
      }
    }
  }
}
