using Microsoft.VisualStudio.TestTools.UnitTesting;
using statsd.net;
using statsd.net.Messages;
using statsd.net.System;
using statsd.net_Tests.Infrastructure;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace statsd.net_Tests
{
  [TestClass]
  public class TimerTests : StatsdTestSuite
  {
    [TestMethod]
    public void WriteOneLatency_OneMetricFlushed_NoPercentiles()
    {
      _statsd.AddAggregator(MessageType.Timing,
        AggregatorFactory.CreateTimedLatencyBlock("stats.timers",
          new TimeSpan(0, 0, 0, 0, 100), 90));
      _listener.Send(_.timing.foo.bar.baz + 100);
      Thread.Sleep(200);
      var messages = _backend.Messages.ToDictionary(p => p.Name);
      Assert.AreEqual(messages["stats.timers.foo.bar.baz.mean"].Quantity, 100);
      Assert.IsFalse(messages.ContainsKey("stats.timers.foo.bar.baz.p90"));
    }

    [TestMethod]
    public void WriteTenLatencies_OneMetricFlushed()
    {
      _statsd.AddAggregator(MessageType.Timing,
        AggregatorFactory.CreateTimedLatencyBlock("stats.timers",
          new TimeSpan(0, 0, 0, 0, 100), 90));
      TestUtility.Range(10).ForEach(p =>
        {
          _listener.Send(_.timing.foo.bar.baz + (p * 10));
        });
      Thread.Sleep(200);
      var messages = _backend.Messages.ToDictionary(p => p.Name);
      Assert.AreEqual(messages["stats.timers.foo.bar.baz.count"].Quantity, 10);
      Assert.AreEqual(messages["stats.timers.foo.bar.baz.min"].Quantity, 0);
      Assert.AreEqual(messages["stats.timers.foo.bar.baz.max"].Quantity, 90);
      Assert.AreEqual(messages["stats.timers.foo.bar.baz.mean"].Quantity, 45);
      Assert.AreEqual(messages["stats.timers.foo.bar.baz.sum"].Quantity, 450);
      Assert.AreEqual(messages["stats.timers.foo.bar.baz.p90"].Quantity, 90);
    }
  }
}
