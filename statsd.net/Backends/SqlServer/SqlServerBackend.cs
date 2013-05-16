using Microsoft.SqlServer.Server;
using statsd.net.Listeners;
using statsd.net.Messages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net.Services;
using statsd.net.Framework;
using Microsoft.Practices.TransientFaultHandling;
using log4net;

namespace statsd.net.Backends.SqlServer
{
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

    public SqlServerBackend(string connectionString, 
      string collectorName,
      ISystemMetricsService systemMetrics,
      int retries = 3,
      int batchSize = 2000)
    {
      _log = SuperCheapIOC.Resolve<ILog>();
      _connectionString = connectionString;
      _collectorName = collectorName;
      _systemMetrics = systemMetrics;
      _retries = retries;

      InitialiseRetryHandling();

      _batchBlock = new BatchBlock<GraphiteLine>(batchSize);
      _actionBlock = new ActionBlock<GraphiteLine[]>(p => SendToDB(p), new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
      _batchBlock.LinkTo(_actionBlock);

      _batchBlock.Completion.ContinueWith(p => _actionBlock.Complete());
      _actionBlock.Completion.ContinueWith(p => { _isActive = false; });

      _completionTask = new Task(() =>
        {
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

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, GraphiteLine messageValue, ISourceBlock<GraphiteLine> source, bool consumeToAccept)
    {
      _batchBlock.Post(messageValue);
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
          _log.Error(String.Format("Retry {0} failed. Trying again. Delay {1}, Error: {2}", args.CurrentRetryCount, args.Delay, args.LastException.Message), args.LastException);
          _systemMetrics.Log("backends.sqlserver.retry");
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

        _retryPolicy.ExecuteAction(() =>
          {
            using (var bulk = new SqlBulkCopy(_connectionString))
            {
              bulk.DestinationTableName = "tb_Metrics";
              bulk.WriteToServer(tableData);
            }
            _systemMetrics.Log("backends.sqlserver.lines", tableData.Rows.Count);
          });
      }
      catch (Exception ex)
      {
        _log.Error("SqlServerBackend: All retries failed.", ex);
        _systemMetrics.Log("backends.sqlserver.droppedData");
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
