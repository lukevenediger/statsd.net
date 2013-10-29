using statsd.net.shared;
using statsd.net.shared.Backends;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.Backends
{
  /// <summary>
  /// Forwards all metrics on to another statsd.net instance over TCP.
  /// </summary>
  public class StatsdnetBackend : IBackend
  {
    private Task _completionTask;
    private bool _isActive;
    private ActionBlock<GraphiteLine> _statsdOutputBlock;
    private ISystemMetricsService _systemMetrics;

    public StatsdnetBackend(string host, int port, ISystemMetricsService systemMetrics)
    {
      _systemMetrics = systemMetrics;
      _completionTask = new Task(() => { _isActive = false; });
      StatsdClient.Statsd client = new StatsdClient.Statsd( host, port, StatsdClient.ConnectionType.Tcp );

      _statsdOutputBlock = new ActionBlock<GraphiteLine>(line =>
        {
          client.LogRaw(line.Name, line.Quantity, line.Epoc);
          _systemMetrics.LogCount("backends.statsdnet.lines");
        },
        Utility.OneAtATimeExecution());

      _isActive = true;
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
      messageValue.FeedTarget(_statsdOutputBlock);
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
