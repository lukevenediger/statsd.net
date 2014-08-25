using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration
{
    public class ListenerConfiguration
    {
    }

    public class UDPListenerConfiguration : ListenerConfiguration
    {
        public int Port { get; set; }
        public UDPListenerConfiguration(int port)
        {
            Port = port;
        }
    }

    public class TCPListenerConfiguration : ListenerConfiguration
    {
        public int Port { get; set; }
        public TCPListenerConfiguration(int port)
        {
            Port = port;
        }
    }

    public class HTTPListenerConfiguration : ListenerConfiguration
    {
        public int Port { get; set; }
        public string HeaderKey { get; set; }
        public HTTPListenerConfiguration(int port, string headerKey = null)
        {
            Port = port;
            HeaderKey = headerKey;
        }
    }

    public class StatsdnetListenerConfiguration : ListenerConfiguration
    {
        public int Port { get; set; }
        public StatsdnetListenerConfiguration(int port)
        {
            Port = port;
        }
    }

    public class MSSQLRelayListenerConfiguration : ListenerConfiguration
    {
        public string ConnectionString { get; set; }
        public int BatchSize { get; set; }
        public bool DeleteAfterSend { get; set; }
        public TimeSpan PollInterval { get; set; }

        public MSSQLRelayListenerConfiguration(string connectionString,
            int batchSize,
            bool deleteAfterSend,
            TimeSpan pollInterval)
        {
            ConnectionString = connectionString;
            BatchSize = batchSize;
            DeleteAfterSend = deleteAfterSend;
            PollInterval = pollInterval;
        }
    }
}
