using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Services
{
    public interface ITimeWindowService
    {
        TimeWindow GetTimeWindow();
    }

    public class TimeWindowService : ITimeWindowService
    {
        public TimeWindow GetTimeWindow()
        {
            return new TimeWindow();
        }
    }
}
