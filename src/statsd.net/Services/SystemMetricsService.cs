using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace statsd.net.Services
{
  public interface ISystemMetricsService
  {
    void ProcessedALine ();
    void SawBadLine ();

    void SentLinesToSqlBackend ( int rows );
  }

  /// <summary>
  /// Keeps track of things like bad lines, failed sends, lines processed etc.
  /// </summary>
  public class SystemMetricsService : ISystemMetricsService
  {
    private int _linesProcessed;
    private int _badLinesSeen;
    private int _sqlBackendLines;
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
  }
}
