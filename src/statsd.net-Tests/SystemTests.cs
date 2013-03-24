using Microsoft.VisualStudio.TestTools.UnitTesting;
using statsd.net;
using statsd.net.Messages;
using statsd.net.System;
using statsd.net_Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace statsd.net_Tests
{
  [TestClass]
  public class SystemTests : StatsdTestSuite
  {
    [TestMethod]
    [Timeout(10 * 1000)] /* 10 seconds */
    public void SystemStartsWithNoTraffic_ShutsDown()
    {
      _statsd.Stop();
    }

    [TestMethod]
    public void SubmitInvalidLine_GetOneBadLineCount()
    {
      _statsd.AddAggregator(MessageType.Counter, AggregatorFactory.CreateTimedCountersBlock("stats_counts", TimeSpan.FromMilliseconds(100)));
      _listener.Send("totally invalid message");
      Thread.Sleep(200);
      Assert.AreEqual(_backend.Messages[0].Name, "stats_counts.statsdnet.badlines");
      Assert.AreEqual(_backend.Messages[0].Quantity, 1);
    }
 }
}
