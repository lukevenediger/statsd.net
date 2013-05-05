using statsd.net.Messages;
using statsd.net.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.Services
{
  public interface ISystemMetricsService
  {
    void ReceivedUDPCall();
    void ReceivedUDPBytes(int numBytesReceived);
    void ProcessedALine();
    void SawBadLine();

    void SentLinesToGraphite(int numLines = 1);
    void SentBytesToGraphite(int numBytes);
    void SentLinesToSqlBackend(int numLines = 1);
    void SentBytesToSqlBackend(int numBytes);
    void SubmitForProcessing();
  }

  /// <summary>
  /// Keeps track of things like bad lines, failed sends, lines processed etc.
  /// </summary>
  public class SystemMetricsService : ISystemMetricsService
  {
    private long _receivedUDPCalls;
    private long _receivedUDPBytes;
    private long _linesProcessed;
    private long _badLinesSeen;
    private long _sentToSQL;
    private long _sentToSQLBytes;
    private long _sentToGraphite;
    private long _sentToGraphiteBytes;

    public SystemMetricsService()
    {
    }

    public void ReceivedUDPCall()
    {
      Interlocked.Increment(ref _receivedUDPCalls);
    }

    public void ReceivedUDPBytes(int numBytesReceived)
    {
      Interlocked.Add(ref _receivedUDPBytes, numBytesReceived);
    }

    public void ProcessedALine()
    {
      Interlocked.Increment(ref _linesProcessed);
    }

    public void SawBadLine()
    {
      Interlocked.Increment(ref _badLinesSeen);
    }

    public void SentLinesToSqlBackend(int numLines = 1)
    {
      Interlocked.Add(ref _sentToSQL, numLines);
    }

    public void SentBytesToSqlBackend(int numByes)
    {
      Interlocked.Add(ref _sentToSQLBytes, numByes);
    }

    public void SentLinesToGraphite(int numLines = 1)
    {
      Interlocked.Add(ref _sentToGraphite, numLines);
    }

    public void SentBytesToGraphite(int numBytes)
    {
      Interlocked.Add(ref _sentToGraphiteBytes, numBytes);
    }

    public void SubmitForProcessing()
    {
      var target = SuperCheapIOC.Resolve<ITargetBlock<GraphiteLine>>();

      long bytesUDP = Interlocked.Exchange(ref _receivedUDPBytes, 0);
      long callsUDP = Interlocked.Exchange(ref _receivedUDPCalls, 0);
      long badLines = Interlocked.Exchange(ref _badLinesSeen, 0);
      long linesProcessed = Interlocked.Exchange(ref _linesProcessed, 0);
      long sentToSql = Interlocked.Exchange(ref _sentToSQL, 0);
      long sentToSqlBytes = Interlocked.Exchange(ref _sentToSQLBytes, 0);
      long sentToGraphite = Interlocked.Exchange(ref _sentToGraphite, 0);
      long sentToGraphiteBytes = Interlocked.Exchange(ref _sentToGraphiteBytes, 0);

      target.Post(new GraphiteLine("statsdnet.listeners.udp.calls", (int)callsUDP));
      target.Post(new GraphiteLine("statsdnet.listeners.udp.bytes", (int)bytesUDP));
      target.Post(new GraphiteLine("statsdnet.lines.badLines", (int)badLines));
      target.Post(new GraphiteLine("statsdnet.lines.processed", (int)linesProcessed));
      target.Post(new GraphiteLine("statsdnet.backends.sql.linesProcessed", (int)sentToSql));
      target.Post(new GraphiteLine("statsdnet.backends.sql.bytes", (int)sentToSqlBytes));
      target.Post(new GraphiteLine("statsdnet.backends.graphite.linesProcessed", (int)sentToGraphite));
      target.Post(new GraphiteLine("statsdnet.backends.graphite.bytes", (int)sentToGraphiteBytes));
    }
  }
}
