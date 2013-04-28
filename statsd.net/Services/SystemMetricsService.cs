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
    void ProcessedALine ();
    void SawBadLine ();

    void SentLinesToSqlBackend ( int rows );
    void SubmitForProcessing();
  }

  /// <summary>
  /// Keeps track of things like bad lines, failed sends, lines processed etc.
  /// </summary>
  public class SystemMetricsService : ISystemMetricsService
  {
    private long _linesProcessed;
    private long _badLinesSeen;
    private long _sqlBackendLines;
    private CancellationToken _cancellationToken;

    public SystemMetricsService (CancellationToken cancellationToken)
    {
      _linesProcessed = _badLinesSeen = _sqlBackendLines = 0;
      _cancellationToken = cancellationToken;
    }

    public void ProcessedALine ()
    {
      Interlocked.Increment( ref _linesProcessed );
    }

    public void SawBadLine ()
    {
      Interlocked.Increment( ref _badLinesSeen );
    }

    public void SentLinesToSqlBackend ( int numberOfLines )
    {
      Interlocked.Add( ref _sqlBackendLines, numberOfLines );
    }

    public void SubmitForProcessing()
    {
      var target = SuperCheapIOC.Resolve<ITargetBlock<GraphiteLine>>();

      long badLines = Interlocked.Exchange(ref _badLinesSeen, 0);
      long linesProcessed = Interlocked.Exchange(ref _linesProcessed, 0);
      long sqlBackendLines = Interlocked.Exchange(ref _sqlBackendLines, 0);

      target.Post(new GraphiteLine("statsdnet.badLinesSeen", (int)_badLinesSeen));
      target.Post(new GraphiteLine("statsdnet.linesSeen", (int)_linesProcessed));
      target.Post(new GraphiteLine("statsdnet.backends.sqlBackend.linesProcessed", (int)_sqlBackendLines));
    }
  }
}
