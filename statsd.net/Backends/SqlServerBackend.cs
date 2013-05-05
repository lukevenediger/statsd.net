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

namespace statsd.net.Backends
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

    public SqlServerBackend(string connectionString, 
      string collectorName,
      ISystemMetricsService systemMetrics)
    {
      _connectionString = connectionString;
      _collectorName = collectorName;
      _systemMetrics = systemMetrics;
      _batchBlock = new BatchBlock<GraphiteLine>(50);
      _actionBlock = new ActionBlock<GraphiteLine[]>(p => SendToDB(p));
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

    private void SendToDB(GraphiteLine[] lines)
    {
      var tableData = lines.Select(p =>
        {
          var entry = new SqlDataRecord(statsdTable);
          entry.SetString(0, p.ToString());
          return entry;
        });
      
      using (var conn = new SqlConnection(_connectionString))
      {
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.pr_Metrics_AddMetrics";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add("@metrics", SqlDbType.Structured);
        cmd.Parameters.AddWithValue("@source", _collectorName);
        cmd.Parameters["@metrics"].Direction = ParameterDirection.Input;
        cmd.Parameters["@metrics"].TypeName = "MetricEntriesTableType";
        cmd.Parameters["@metrics"].Value = tableData;
        int rows = cmd.ExecuteNonQuery();
        _systemMetrics.SentLinesToSqlBackend( rows );
      }

      _systemMetrics.SentLinesToSqlBackend(lines.Length);
    }
  }
}
