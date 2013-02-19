using statsd.net.Listeners;
using statsd.net.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net
{
  class Program
  {
    static void Main(string[] args)
    {
      var timedBlock = TimedDataBlockFactory.CreateTimedBlock(new TimeSpan(0, 1, 0));
      var udpListener = new UdpStatsListener(12000, timedBlock);
    }
  }
}
