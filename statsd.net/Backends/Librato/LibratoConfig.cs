using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Backends.Librato
{
  [DebuggerDisplay("{Email} - {Source}")]
  public class LibratoConfig
  {
    public string Email { get; set; }
    public string Token { get; set; }
    public string Source { get; set; }
    public bool SkipInternalMetrics { get; set; }
    public int RetryDelaySeconds { get; set; }
    public int PostTimeoutSeconds { get; set; }
    public int MaxBatchSize { get; set; }
    public string Api { get; set; }
  }
}
