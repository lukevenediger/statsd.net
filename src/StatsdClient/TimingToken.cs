using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsdClient
{
  public sealed class TimingToken : IDisposable
  {
    private IStatsdClient _client;
    private string _name;
    private Stopwatch _stopwatch;

    internal TimingToken(IStatsdClient client, string name)
    {
      _stopwatch = Stopwatch.StartNew();
      _client = client;
      _name = name;
    }

    public void Dispose()
    {
      _stopwatch.Stop();
      _client.LogTiming(_name, (int)_stopwatch.ElapsedMilliseconds);
    }
  }
}
