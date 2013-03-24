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
  public class CounterTests : StatsdTestSuite
  {
    [TestMethod]
    public void WriteOneCounter_OneMetricIsFlushed()
    {
      _statsd.AddAggregator(MessageType.Counter, AggregatorFactory.CreateTimedCountersBlock("", new TimeSpan(0, 0, 0, 0, 100)));
      var stat = _.count.foo.bar.baz + 1;
      _listener.Send(stat);
      Thread.Sleep(200);
      Assert.AreEqual(1, _backend.Messages.Last().Quantity);
    }

    [TestMethod]
    public void WriteTenCounters_OneMetricIsFlushed()
    {
      _statsd.AddAggregator(MessageType.Counter, AggregatorFactory.CreateTimedCountersBlock("", new TimeSpan(0, 0, 0, 0, 100)));
      TestUtility.Range(10).ForEach(p =>
      {
        _listener.Send(_.count.foo.bar.baz + 1);
      });
      Thread.Sleep(200);
      Assert.AreEqual(10, _backend.Messages[0].Quantity);
    }

    [TestMethod]
    public void WriteTwoCounters_TwoMetricsFlushed()
    {
      _statsd.AddAggregator(MessageType.Counter, AggregatorFactory.CreateTimedCountersBlock("", new TimeSpan(0, 0, 0, 0, 100)));
      _listener.Send(_.count.foo.bar.baz + 1);
      _listener.Send(_.count.foo.bar.beans + 1);
      Thread.Sleep(200);
      Assert.AreEqual(2, _backend.Messages.Count);
    }
  }
}
