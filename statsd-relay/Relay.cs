using log4net;
using statsd.net.shared;
using statsd.net.shared.Factories;
using statsd.net.shared.Listeners;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.relay
{
  public class Relay
  {
    private CancellationTokenSource _tokenSource;
    private ManualResetEvent _shutdownComplete;
    private List<IListener> _listeners;
    private static readonly ILog _log = LogManager.GetLogger("statsdrelay");

    public WaitHandle ShutdownWaitHandle
    {
      get { return _shutdownComplete; }
    }

    public Relay(dynamic config)
    {
      LoggingBootstrap.Configure();
      _log.Info("statsdrelay is starting up.");
      _tokenSource = new CancellationTokenSource();
      _shutdownComplete = new ManualResetEvent(false);
      _listeners = new List<IListener>();

      var systemInfoService = new SystemInfoService();
      var relayMetrics = new RelayMetricsService("relay", _tokenSource.Token, systemInfoService.HostName);
      SuperCheapIOC.Add(relayMetrics as ISystemMetricsService);

      /* Pipeline is
       *  UDPStatsListener
       *  HTTPStatsListener
       *  TCPStatsListener
       *    ->  MessageParserBlock
       *      ->  BatchBlock
       *        -> UDPRawStatsSender
       */

      var udpSender = new UDPRawStatsSender(config.target.host, (int)config.target.port, relayMetrics);
      var outputBlock = new ActionBlock<StatsdMessage[]>((lines) =>
        {
          // Only send valid lines
          _log.InfoFormat("Forwarding {0} lines.", lines.Length);
          udpSender.Send(lines.Where(p => !(p is InvalidMessage)).ToArray());
        },
        new ExecutionDataflowBlockOptions()
      {
        BoundedCapacity = ExecutionDataflowBlockOptions.Unbounded,
        CancellationToken = _tokenSource.Token
      });
      var batchBlock = new BatchBlock<StatsdMessage>(10, new GroupingDataflowBlockOptions()
      {
        CancellationToken = _tokenSource.Token,
        BoundedCapacity = GroupingDataflowBlockOptions.Unbounded
      });
      batchBlock.LinkTo(outputBlock);
      var messageParserBlock = MessageParserBlockFactory.CreateMessageParserBlock(_tokenSource.Token, relayMetrics, _log);
      messageParserBlock.LinkTo(batchBlock);

      // Completion chain
      messageParserBlock.Completion.LogAndContinueWith(_log, "MessageParserBlock",
        () =>
        {
          _log.Info("MessageParserBlock: Completion signalled. Notifying BatchBlock.");
          batchBlock.Complete();
        });

      batchBlock.Completion.LogAndContinueWith(_log, "BatchBlock",
        () =>
        {
          _log.Info("BatchBlock: Completion signalled. Notifying OutputBlock.");
          outputBlock.Complete();
        });
      outputBlock.Completion.LogAndContinueWith(_log, "OutputBlock",
        () =>
        {
          // Last one to leave the room turns out the lights.
          _log.Info("OutputBlock: Completion signalled. Shutting down.");
          _shutdownComplete.Set();
        });

      // Listeners
      if (config.listeners.udp.enabled)
      {
        var udpListener = new UdpStatsListener((int)config.listeners.udp.port, relayMetrics);
        udpListener.LinkTo(messageParserBlock, _tokenSource.Token);
        _listeners.Add(udpListener);
      }
      if (config.listeners.http.enabled)
      {
        var httpListener = new HttpStatsListener2((int)config.listeners.http.port, relayMetrics);
        httpListener.LinkTo(messageParserBlock, _tokenSource.Token);
        _listeners.Add(httpListener);
      }
      if (config.listeners.tcp.enabled)
      {
        var tcpListener = new TcpStatsListener((int)config.listeners.tcp.port, relayMetrics);
        tcpListener.LinkTo(messageParserBlock, _tokenSource.Token);
        _listeners.Add(tcpListener);
      }

      // Set the system metrics target
      relayMetrics.SetTarget(batchBlock);
    }

    public void Stop()
    {
      _tokenSource.Cancel();
      while (_listeners.Any(x => x.IsListening))
      {
        // Probably better to use wait handles on the listener here.
        Thread.Sleep(100);
      }
      // Wait for all the blocks to finish up.
      _shutdownComplete.WaitOne();
      _log.Info("Done.");
    }
  }
}
