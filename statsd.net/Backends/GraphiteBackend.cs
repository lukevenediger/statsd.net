using System.ComponentModel.Composition;
using System.Net;
using System.Xml.Linq;
using statsd.net.Configuration;
using statsd.net.core;
using statsd.net.core.Backends;
using statsd.net.core.Messages;
using statsd.net.core.Structures;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net.shared;
using log4net;

namespace statsd.net.Backends
{
    [Export(typeof(IBackend))]
    public class GraphiteBackend : IBackend
    {
        private Task _completionTask;
        private bool _isActive;
        private ISystemMetricsService _systemMetrics;
        private ILog _log;
        private ActionBlock<GraphiteLine> _senderBlock;

        public string Name { get { return "Graphite"; } }

        private IGraphiteCommunicator graphiteCommunicator;

        public void Configure(string collectorName, XElement configElement, ISystemMetricsService systemMetrics)
        {
            _log = SuperCheapIOC.Resolve<ILog>();
            _systemMetrics = systemMetrics;
            _completionTask = new Task(() => { _isActive = false; });
            _senderBlock = new ActionBlock<GraphiteLine>((message) => SendLine(message), Utility.UnboundedExecution());
            _isActive = true;

            var config = new GraphiteConfiguration(configElement.Attribute("host").Value, configElement.ToInt("port"), configElement.Attribute("protocol").Value);
            switch (config.GraphiteCommunicationProtocol.ToUpper())
            {
                case "UDP":
                    graphiteCommunicator = new GraphiteUdpCommunicator(config);
                    break;

                case "TCP":
                    graphiteCommunicator = new GraphiteTcpCommunicator(config);
                    break;
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
            try
            {
                graphiteCommunicator.SendLine(line);
                _systemMetrics.LogCount("backends.graphite.lines");
            }
            catch (SocketException ex)
            {
                _log.Error("Failed to send packet to Graphite: " + ex.SocketErrorCode.ToString());
            }
        }
    }



    public interface IGraphiteCommunicator
    {
        void SendLine(GraphiteLine line);
    }

    public class GraphiteTcpCommunicator : IGraphiteCommunicator
    {
        private readonly GraphiteConfiguration configuration;
        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;

        public GraphiteTcpCommunicator(GraphiteConfiguration configuration)
        {
            this.configuration = configuration;
            _tcpClient = new TcpClient();
            var ipAddress = Utility.HostToIPv4Address(configuration.Host);
            _tcpClient.Connect(ipAddress, configuration.Port);
            _tcpStream = _tcpClient.GetStream();
        }

        public void SendLine(GraphiteLine line)
        {
            byte[] data = Encoding.ASCII.GetBytes((line + "\r\n"));
            _tcpStream.Write(data, 0, data.Length);
            _tcpStream.Flush();
        }
    }

    public class GraphiteUdpCommunicator : IGraphiteCommunicator
    {
        private readonly GraphiteConfiguration configuration;
        private UdpClient _client;

        public GraphiteUdpCommunicator(GraphiteConfiguration configuration)
        {
            this.configuration = configuration;
            var ipAddress = Utility.HostToIPv4Address(configuration.Host);
            _client = new UdpClient();
            _client.Connect(ipAddress, configuration.Port);
        }

        public void SendLine(GraphiteLine line)
        {
            byte[] data = Encoding.ASCII.GetBytes((line + "\r\n"));
            _client.Send(data, data.Length);
        }
    }
}
