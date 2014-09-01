using statsd.net.core;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Listeners
{
    /// <summary>
    /// Listens out for raw metrics in a SQL db and feeds them
    /// in as raw metrics.
    /// </summary>
    public class MSSQLRelayListener : IListener
    {
        private static string[] SPACE_SPLITTER = new String[] { " " };

        private string _connectionString;
        private IntervalService _intervalService;
        private int _batchSize;
        private bool _deleteAfterSend;
        private ISystemMetricsService _metrics;
        private ITargetBlock<string> _target;
        private CancellationToken _cancellationToken;

        public MSSQLRelayListener(string connectionString, 
            TimeSpan pollInterval,
            CancellationToken cancellationToken,
            int batchSize,
            bool deleteAfterSend,
            ISystemMetricsService metrics)
        {
            _connectionString = connectionString;
            _intervalService = new IntervalService(pollInterval, cancellationToken);
            _cancellationToken = cancellationToken;
            _batchSize = batchSize;
            _deleteAfterSend = deleteAfterSend;
            _metrics = metrics;

            var stopwatch = new Stopwatch();

            _intervalService.Elapsed += (sender, e) =>
                {
                    if (IsListening)
                    {
                        _intervalService.Cancel(true);
                        stopwatch.Restart();
                        ReadAndFeed();
                        stopwatch.Stop();
                        metrics.LogCount("listeners.mssql-relay.feedTimeSeconds", Convert.ToInt32(stopwatch.Elapsed.TotalSeconds));

                        // Only continue the interval service if cancellation
                        // isn't in progress
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            _intervalService.Start();
                        }
                    }
                };
        }

        public void LinkTo(ITargetBlock<string> target, CancellationToken token)
        {
            _target = target;
            IsListening = true;
            _intervalService.Start();
        }

        public bool IsListening { get; private set; }

        private void ReadAndFeed()
        {
            try
            {
                _metrics.LogCount("listeners.mssql-relay.feed.attempt");
                var lines = GetNewLinesFromDB();
                foreach (String line in lines)
                {
                    var parts = line.Split(SPACE_SPLITTER, StringSplitOptions.RemoveEmptyEntries);
                    _target.Post(parts[0] + ":" + parts[1] + "|r|" + parts[2]);
                }
                _metrics.LogCount("listeners.mssql-relay.lines.posted" + lines.Count);
                _metrics.LogCount("listeners.mssql-relay.feed.success");
            } 
            catch (Exception ex)
            {
                _metrics.LogCount("listeners.mssql-relay.error." + ex.GetType().Name);
                _metrics.LogCount("listeners.mssql-relay.feed.failure");
            }
        }

        private List<String> GetNewLinesFromDB()
        {
            using (var conn = new SqlConnection( _connectionString ))
            {
                conn.Open();
                var lastRowID = GetLastRowID(conn);
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = 
                    String.Format(
                        "SELECT TOP {0} measure, rowid FROM tb_metrics WHERE rowid > {1} ORDER BY rowid ASC",
                        _batchSize,
                        lastRowID
                    );
                cmd.CommandType = CommandType.Text;
                lastRowID = 0;
                var counter = 0;

                var rows = new List<String>();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    string row;
                    while (reader.Read())
                    {
                        row = reader.GetString(0);
                        lastRowID = reader.GetInt64(1);
                        rows.Add(row);
                        counter++;
                    }
                }

                _metrics.LogCount("listeners.mssql-relay.action.fetchNewRows");
                _metrics.LogCount("listeners.mssql-relay.lines.fetched" + rows.Count);

                // Make note of the last row ID we updated
                if (counter > 0)
                {
                    UpdateLastRowID(conn, lastRowID);

                    if (_deleteAfterSend)
                    {
                        DeleteProcessedRecords(conn, lastRowID);
                    }
                }

                return rows;
            }
        }

        private long GetLastRowID(SqlConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ISNULL(lastRowId, -1) FROM tb_metricscontrol";
            cmd.CommandType = CommandType.Text;
            var result = cmd.ExecuteScalar();
            _metrics.LogCount("listeners.mssql-relay.action.getLastRowID");
            return result == null ? 0 : (long)result;
        }

        private void UpdateLastRowID(SqlConnection conn, long lastRowId)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText =
                String.Format(
                    "IF EXISTS (SELECT 1 FROM tb_metricscontrol) UPDATE tb_metricscontrol SET lastRowID = {0} " +
                    "ELSE INSERT INTO tb_metricscontrol (lastRowID) VALUES ({0})",
                    lastRowId);
            cmd.CommandType = CommandType.Text;
            cmd.ExecuteNonQuery();
            _metrics.LogCount("listeners.mssql-relay.action.updateLastRowID");
        }

        private int DeleteProcessedRecords(SqlConnection conn, long lastRowId)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = String.Format("DELETE FROM tb_metrics WHERE rowid < {0}", lastRowId);
            cmd.CommandType = CommandType.Text;
            var rowsDeleted = cmd.ExecuteNonQuery();
            _metrics.LogCount("listeners.mssql-relay.action.deleteProcessedRecords");
            _metrics.LogCount("listeners.mssql-relay.rowsDeleted", rowsDeleted);
            return rowsDeleted;
        }
    }
}
