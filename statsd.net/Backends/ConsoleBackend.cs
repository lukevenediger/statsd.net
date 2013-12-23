using System.ComponentModel.Composition;
using System.Xml.Linq;
using statsd.net.Configuration;
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
  [Export(typeof(IBackend))]
  public class ConsoleBackend : IBackend
  {
    private bool _isActive;
    private Task _completionTask;

    public string Name { get { return "Console"; } }  

    public void Configure(string collectorName, XElement configElement, ISystemMetricsService systemMetrics)
    {
      _isActive = true;
      _completionTask = new Task(() => { _isActive = false; });
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, Bucket bucket, ISourceBlock<Bucket> source, bool consumeToAccept)
    {
      Console.WriteLine(bucket.ToString());
      return DataflowMessageStatus.Accepted;
    }

    public void Complete()
    {
      Console.WriteLine("Done");
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

    public bool IsActive
    {
      get { return _isActive; }
    }

    public int OutputCount
    {
      get { return 0; }
    }

  }
}
