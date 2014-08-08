using statsd.net.shared.Services;
using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net_Tests.Infrastructure
{
    public class MockTimeWindowService : ITimeWindowService
    {
        private DateTime _nextDateTime { get; set; }

        public MockTimeWindowService(DateTime startDateTime)
        {
            _nextDateTime = startDateTime;
        }
        
        public TimeWindow GetTimeWindow()
        {
            return new TimeWindow(new DateTime(_nextDateTime.Ticks));
        }

        public void AddHours(int numHours)
        {
            _nextDateTime = _nextDateTime.AddHours(numHours);
        }

        internal void AddDays(int numDays)
        {
            _nextDateTime = _nextDateTime.AddDays(numDays);
        }

        internal void AddMonths(int numMonths)
        {
            _nextDateTime = _nextDateTime.AddMonths(numMonths);
        }

        internal void AddMinutes(int numMinutes)
        {
            _nextDateTime = _nextDateTime.AddMinutes(numMinutes);
        }
    }
}
