using statsd.net.shared.Services;
using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Listeners
{
    public class MSSQLRelayListener : IListener
    {
        private static string[] SPACE_SPLITTER = new String[] { " " };

        private string _connectionString;
        private IIntervalService _intervalService;
        private int _batchSize;
        private bool _deleteAfterSend;

        private BufferBlock<RelayMetric> _buffer;
        private ActionBlock<RelayMetric[]> _feeder;
        private ITargetBlock<string> _target;

        public MSSQLRelayListener(string connectionString, 
            IIntervalService intervalService,
            int batchSize, 
            bool deleteAfterSend) 
        {
            _connectionString = connectionString;
            _intervalService = intervalService;
            _batchSize = batchSize;
            _deleteAfterSend = deleteAfterSend;
            _buffer = new BufferBlock<RelayMetric>();
            _feeder = new ActionBlock<RelayMetric[]>(p => FeedMetrics(p));
        }

        private Task FeedMetrics(RelayMetric[] metrics)
        {
            foreach(RelayMetric metric in metrics)
            {
                var parts = line.Split(SPACE_SPLITTER, StringSplitOptions.RemoveEmptyEntries);
                _target.Post(parts[0] + ":" + parts[1] + "|r|" + parts[2]);
            }
        }

        public void LinkTo(ITargetBlock<string> target, CancellationToken token)
        {
            _target = target;
        }

        public bool IsListening
        {
            get { throw new NotImplementedException(); }
        }
    }
}
