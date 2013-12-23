using System.ComponentModel.Composition;
using System.Xml.Linq;
using statsd.net.Configuration;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net.shared;
using statsd.net.shared.Backends;
using log4net;
using statsd.net.shared.Structures;

namespace statsd.net.Backends
{
  [Export(typeof(IBackend))]
  public class GraphiteBackend : IBackend
  {
    private UdpClient _client;
    private Task _completionTask;
    private bool _isActive;
    private ISystemMetricsService _systemMetrics;
    private ILog _log;
    private ActionBlock<GraphiteLine> _senderBlock;

    public string Name { get { return "Graphite"; } }  

    public void Configure(string collectorName, XElement configElement, ISystemMetricsService systemMetrics)
    {
      _log = SuperCheapIOC.Resolve<ILog>();
      _systemMetrics = systemMetrics;
      _completionTask = new Task(() => { _isActive = false; });
      _senderBlock = new ActionBlock<GraphiteLine>((message) => SendLine(message), Utility.UnboundedExecution());
      _isActive = true;

      var config = new GraphiteConfiguration(configElement.Attribute("host").Value, configElement.ToInt("port"));

      var ipAddress = Utility.HostToIPv4Address(config.Host);
      _client = new UdpClient();
      _client.Connect(ipAddress, config.Port);
    }

    public bool IsActive
    {
      get { return _isActive; }
    }

    public int OutputCount
    {
      get { return 0; }
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, Bucket messageValue, ISourceBlock<Bucket> source, bool consumeToAccept)
    {
      messageValue.FeedTarget(_senderBlock);
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

    private void SendLine(GraphiteLine line)
    {
      byte[] data = Encoding.ASCII.GetBytes(line.ToString());
      try
      {
        _client.Send(data, data.Length);
        _systemMetrics.LogCount("backends.graphite.lines");
        _systemMetrics.LogCount("backends.graphite.bytes", data.Length);
      }
      catch (SocketException ex)
      {
        _log.Error("Failed to send packet to Graphite: " + ex.SocketErrorCode.ToString());
      }
    }
  }
}
