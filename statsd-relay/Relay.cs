using statsd.net.shared;
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
    public WaitHandle ShutdownWaitHandle { get; set; }
    private CancellationTokenSource _tokenSource;

    public Relay(dynamic config)
    {
      _tokenSource = new CancellationTokenSource();
      var systemInfoService = new SystemInfoService();
      var systemMetrics = new SystemMetricsService("relay", systemInfoService.HostName);
      SuperCheapIOC.Add(systemMetrics as ISystemMetricsService);

      var udpSender = new UDPRawStatsSender(config.target.host, (int)config.target.port, systemMetrics);
      var outputBlock = new ActionBlock<string[]>((lines) => udpSender.Send(lines), new ExecutionDataflowBlockOptions()
      {
        BoundedCapacity = ExecutionDataflowBlockOptions.Unbounded,
        CancellationToken = _tokenSource.Token
      });
      var batchBlock = new BatchBlock<string>(10, new GroupingDataflowBlockOptions()
      {
        CancellationToken = _tokenSource.Token,
        BoundedCapacity = GroupingDataflowBlockOptions.Unbounded
      });
      batchBlock.LinkTo(outputBlock);
      var filterBlock = new ActionBlock<string>((line) =>
        {
          if (StatsdMessageFactory.IsProbablyAValidMessage(line))
          {
            batchBlock.Post(line);
          }
          systemMetrics.Log("badLinesSeen", 1);
        },
        new ExecutionDataflowBlockOptions()
      {
        BoundedCapacity = ExecutionDataflowBlockOptions.Unbounded,
        CancellationToken = _tokenSource.Token
      });

      // Listeners
      if (config.listeners.udp.enabled)
      {
        var udpListener = new UdpStatsListener((int)config.listeners.udp.port, systemMetrics);
        udpListener.LinkTo(filterBlock, _tokenSource.Token);
      }
      if (config.listeners.http.enabled)
      {
        var httpListener = new HttpStatsListener((int)config.listeners.http.port, systemMetrics);
        httpListener.LinkTo(filterBlock, _tokenSource.Token);
      }
      if (config.listeners.tcp.enabled)
      {
        var tcpListener = new TcpStatsListener((int)config.listeners.tcp.port, systemMetrics);
        tcpListener.LinkTo(filterBlock, _tokenSource.Token);
      }
    }

    public void Stop()
    {
      _tokenSource.Cancel();
    }
  }
}
