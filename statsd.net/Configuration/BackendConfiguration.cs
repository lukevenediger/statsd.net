using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration
{
  public class BackendConfiguration
  {
  }

  public class SqlServerConfiguration : BackendConfiguration
  {
    public string ConnectionString { get; set; }
    public int WriteBatchSize { get; set; }
    public SqlServerConfiguration(string connectionString, int writeBatchSize)
    {
      ConnectionString = connectionString;
      WriteBatchSize = writeBatchSize;
    }
  }

  public class GraphiteConfiguration : BackendConfiguration
  {
    public string Host { get; set; }
    public int Port { get; set; }

    public GraphiteConfiguration(string host, int port)
    {
      Host = host;
      Port = port;
    }
  }

  public class ConsoleConfiguration : BackendConfiguration
  {
  }

  public class LibratoBackendConfiguration : BackendConfiguration
  {
    public string Email { get; set; }
    public string Token { get; set; }
    public TimeSpan RetryDelay { get; set; }
    public TimeSpan PostTimeout { get; set; }
    public int MaxBatchSize { get; set; }
    public bool CountersAsGauges { get; set; }
    public int NumRetries { get; set; } 

    public LibratoBackendConfiguration(string email, string token, TimeSpan retryDelay, int numRetries, TimeSpan postTimeout, int maxBatchSize, bool countersAsGauges)
    {
      this.Email = email;
      this.Token = token;
      this.RetryDelay = retryDelay;
      this.NumRetries = numRetries;
      this.PostTimeout = postTimeout;
      this.MaxBatchSize = maxBatchSize;
      this.CountersAsGauges = countersAsGauges;
    }
  }

  public class StatsdBackendConfiguration : BackendConfiguration
  {
    public string Host { get; set; }
    public int Port { get; set; }

    public StatsdBackendConfiguration(string host, int port)
    {
      Host = host;
      Port = port;
    }
  }

}
