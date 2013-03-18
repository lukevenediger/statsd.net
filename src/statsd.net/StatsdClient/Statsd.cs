using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsdClient
{
  public class StatsdClient : IStatsdClient
  {
    private string _prefix;
    private IOutputChannel _outputChannel;

    public StatsdClient(string host, int port, string prefix = null, bool rethrowOnError = false)
    {
      _prefix = prefix;
      try
      {
        _outputChannel = new UdpOutputChannel(host, port);
      }
      catch (Exception ex)
      {
        if (rethrowOnError)
        {
          throw;
        }
        Trace.TraceError("Could not initialise the Statsd client at {0}:{1} - {2}", host, port, ex.Message);
        _outputChannel = new NullOutputChannel();
      }
    }

    public void LogCount(string name, int count = 1)
    {
      SendMetric(MetricType.COUNT, name, _prefix, count);
    }

    public void LogTiming(string name, int milliseconds)
    {
      SendMetric(MetricType.TIMING, name, _prefix, milliseconds);
    }

    public void LogGauge(string name, int value)
    {
      SendMetric(MetricType.GAUGE, name, _prefix, value);
    }

    private void SendMetric(string metricType, string name, string prefix, int value)
    {
      _outputChannel.Send(PrepareMetric(metricType, name, prefix, value));
    }

    protected virtual string PrepareMetric(string metricType, string name, string prefix, int value)
    {
      return (prefix == null ? (prefix + "." + name) : name) + ":" + value + "|" + metricType;
    }
  }
}
