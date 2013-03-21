using Microsoft.VisualStudio.TestTools.UnitTesting;
using statsd.net.Messages;
using statsd.net.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using statsd.net_Tests.Infrastructure;
using System.Threading;
using StatsdClient;

namespace statsd.net_Tests
{
  [TestClass]
  public class GaugeTests : StatsdTestSuite
  {
    [TestMethod]
    public void WriteOneGauge_OneGaugeIsFlushed()
    {
      _statsd.AddAggregator(MessageType.Gauge, AggregatorFactory.CreateTimedGaugesBlock("stats.gauges", new TimeSpan(0, 0, 0, 0, 100)));
      _listener.Send(_.gauge.foo + 1);
      Thread.Sleep(200);
      Assert.AreEqual(_backend.Messages[0].Name, "stats.gauges.foo");
      Assert.AreEqual(_backend.Messages[0].Quantity, 1);
    }
  }
}
