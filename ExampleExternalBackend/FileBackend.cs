using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using statsd.net.core;
using statsd.net.core.Backends;
using statsd.net.core.Structures;

namespace ExampleExternalBackend
{
  [Export(typeof(IBackend))]
  public class FileBackend : IBackend
  {
    private bool _isActive;
    private Task _completionTask;
    private string _outputFileName;

    public string Name { get { return "File"; } }

    public void Configure(string collectorName, XElement configElement, ISystemMetricsService systemMetrics)
    {
      _isActive = true;
      _completionTask = new Task(() => { _isActive = false; });

      var fileNameAttribute = configElement.Attribute("filename");
      _outputFileName = (fileNameAttribute != null) ? fileNameAttribute.Value : "FileBackend.txt";
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, Bucket bucket, ISourceBlock<Bucket> source, bool consumeToAccept)
    {
      File.AppendAllText(_outputFileName, bucket + Environment.NewLine);
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
