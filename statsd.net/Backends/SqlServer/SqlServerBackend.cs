using System.ComponentModel.Composition;
using System.Xml.Linq;
using Microsoft.SqlServer.Server;
using statsd.net.Configuration;
using statsd.net.core;
using statsd.net.core.Backends;
using statsd.net.core.Messages;
using statsd.net.core.Structures;
using statsd.net.shared.Listeners;
using statsd.net.shared.Messages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net.shared.Services;
using statsd.net.Framework;
using Microsoft.Practices.TransientFaultHandling;
using log4net;
using statsd.net.shared;
using statsd.net.shared.Structures;

namespace statsd.net.Backends.SqlServer
{
  [Export(typeof(IBackend))]
  public class SqlServerBackend : IBackend
  {
    private string _connectionString;
    private string _collectorName;
    private bool _isActive;
    private Task _completionTask;
    private BatchBlock<GraphiteLine> _batchBlock;
    private ActionBlock<GraphiteLine[]> _actionBlock;
    private static SqlMetaData[] statsdTable = { new SqlMetaData("measure", SqlDbType.VarChar, 255) };
    private ISystemMetricsService _systemMetrics;
    private int _retries;
    private Incremental _retryStrategy;
    private RetryPolicy<SqlServerErrorDetectionStrategy> _retryPolicy;
    private ILog _log;

    public string Name { get { return "SqlServer"; } }  

    public void Configure(string collectorName, XElement configElement, ISystemMetricsService systemMetrics)
    {
      _log = SuperCheapIOC.Resolve<ILog>();
      _systemMetrics = systemMetrics;

      var config = new SqlServerConfiguration(configElement.Attribute("connectionString").Value, configElement.ToInt("writeBatchSize"));

      _connectionString = config.ConnectionString;
      _collectorName = collectorName;
      _retries = config.Retries;

      InitialiseRetryHandling();

      _batchBlock = new BatchBlock<GraphiteLine>(config.WriteBatchSize);
      _actionBlock = new ActionBlock<GraphiteLine[]>(p => SendToDB(p), new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
      _batchBlock.LinkTo(_actionBlock);

      _batchBlock.Completion.ContinueWith(p => _actionBlock.Complete());
      _actionBlock.Completion.ContinueWith(p => { _isActive = false; });

      _completionTask = new Task(() =>
      {
        _log.Info("SqlServerBackend - Completion has been signaled. Waiting for action block to complete.");
        _batchBlock.Complete();
        _actionBlock.Completion.Wait();
      });

    }

    public bool IsActive
    {
      get { return _isActive; }
    }

    public int OutputCount
    {
      get { return _batchBlock.OutputCount; }
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, Bucket messageValue, ISourceBlock<Bucket> source, bool consumeToAccept)
    {
      messageValue.FeedTarget(_batchBlock);
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

    private void InitialiseRetryHandling()
    {
      _retryStrategy = new Incremental(_retries, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
      _retryPolicy = new RetryPolicy<SqlServerErrorDetectionStrategy>(_retries);
      _retryPolicy.Retrying += (sender, args) =>
        {
          _log.Warn(String.Format("Retry {0} failed. Trying again. Delay {1}, Error: {2}", args.CurrentRetryCount, args.Delay, args.LastException.Message), args.LastException);
          _systemMetrics.LogCount("backends.sqlserver.retry");
        };
    }

    private void SendToDB(GraphiteLine[] lines)
    {
      try
      {
        DataRow row;
        DataTable tableData = CreateEmptyTable();
        foreach (var line in lines)
        {
          row = tableData.NewRow();
          row["rowid"] = System.DBNull.Value;
          row["source"] = this._collectorName;
          row["metric"] = line.ToString();
          tableData.Rows.Add(row);
        }
        _log.DebugFormat("Attempting to send {0} lines to tb_Metrics.", tableData.Rows.Count);

        _retryPolicy.ExecuteAction(() =>
          {
            using (var bulk = new SqlBulkCopy(_connectionString))
            {
              bulk.DestinationTableName = "tb_Metrics";
              bulk.WriteToServer(tableData);
            }
            _systemMetrics.LogCount("backends.sqlserver.lines", tableData.Rows.Count);
            _log.DebugFormat("Wrote {0} lines to tb_Metrics.", tableData.Rows.Count);
          });
      }
      catch (Exception ex)
      {
        _log.Error("SqlServerBackend: All retries failed.", ex);
        _systemMetrics.LogCount("backends.sqlserver.droppedData");
      }
    }

    public DataTable CreateEmptyTable()
    {
      DataTable outputTable = new DataTable();
      outputTable.Columns.Add("rowid", typeof(int));
      outputTable.Columns.Add("source", typeof(string));
      outputTable.Columns.Add("metric", typeof(string));
      return outputTable;
    }
  }
}
