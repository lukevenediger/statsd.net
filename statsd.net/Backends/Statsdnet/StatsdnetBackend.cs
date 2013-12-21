using System.Xml.Linq;
using log4net;
using log4net.Core;
using statsd.net.Configuration;
using statsd.net.shared;
using statsd.net.shared.Backends;
using statsd.net.shared.Blocks;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.Backends.Statsdnet
{
  /// <summary>
  /// Forwards all metrics on to another statsd.net instance over TCP.
  /// </summary>
  public class StatsdnetBackend : IBackend
  {
    private Task _completionTask;
    private bool _isActive;
    private TimedBufferBlock<GraphiteLine[]> _bufferBlock;
    private ISystemMetricsService _systemMetrics;
    private StatsdnetForwardingClient _client;

    public StatsdnetBackend(string host, int port, TimeSpan flushPeriod, ISystemMetricsService systemMetrics)
    {
      _systemMetrics = systemMetrics;
      var log = SuperCheapIOC.Resolve<ILog>();
      Configure(host, port, flushPeriod);
    }

    public StatsdnetBackend()
    {
    }

    private void Configure(string host, int port, TimeSpan flushPeriod)
    {
      _client = new StatsdnetForwardingClient(host, port, _systemMetrics);
      _bufferBlock = new TimedBufferBlock<GraphiteLine[]>(flushPeriod, PostMetrics);

      _completionTask = new Task(() =>
      {
        _isActive = false;
      });

      _isActive = true;
      
    }

    public void Configure(string collectorName, XElement configElement)
    {
      var config = new StatsdBackendConfiguration(configElement.Attribute("host").Value,
        configElement.ToInt("port"),
        Utility.ConvertToTimespan(configElement.Attribute("flushInterval").Value),
        configElement.ToBoolean("enableCompression", true));

      Configure(config.Host, config.Port, config.FlushInterval);

    }

    private void PostMetrics(GraphiteLine[][] lineArrays)
    {
      var lines = new List<GraphiteLine>();
      foreach(var graphiteLineArray in lineArrays)
      {
        foreach (var line in graphiteLineArray)
        {
          lines.Add(line);
        }
      }
      var rawText = String.Join(Environment.NewLine,
        lines.Select(line => line.ToString()).ToArray());
      var bytes = Encoding.UTF8.GetBytes(rawText);
      if (_client.Send(bytes))
      {
        _systemMetrics.LogCount("backends.statsdnet.lines", lines.Count);
        _systemMetrics.LogGauge("backends.statsdnet.bytes", bytes.Length);
      }
    }

    public bool IsActive
    {
      get { return _isActive; }
    }

    public int OutputCount
    {
      get { return 0; }
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, 
      Bucket messageValue, 
      ISourceBlock<Bucket> source, 
      bool consumeToAccept)
    {
      var lines = messageValue.ToLines();
      _bufferBlock.Post(lines);

      return DataflowMessageStatus.Accepted;
    }

    public void Complete()
    {
      _completionTask.Start();
    }

    public Task Completion
    {
      get { return _completionTask; }
    }

    public void Fault(Exception exception)
    {
      throw new NotImplementedException();
    }
  }
}
