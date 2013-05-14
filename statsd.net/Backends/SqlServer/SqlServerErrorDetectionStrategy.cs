using Microsoft.Practices.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Backends.SqlServer
{
  internal class SqlServerErrorDetectionStrategy : ITransientErrorDetectionStrategy
  {
    public bool IsTransient(Exception ex)
    {
      return ex is SqlException;
    }
  }
}
