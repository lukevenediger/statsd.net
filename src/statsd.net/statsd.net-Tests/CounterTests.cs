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
  public class CounterTests
  {
    private Statsd _statsd;
    private InAppListener _listener;
    private InAppBackend _backend;
    
    [TestInitialize]
    public void Setup()
    {
      _statsd = new Statsd();
      _listener = new InAppListener();
      _backend = new InAppBackend();
      _statsd.AddListener(_listener);
      _statsd.AddBackend(_backend);
    }

    [TestCleanup]
    public void Teardown()
    {
      _statsd.Stop();
    }

    [TestMethod]
    public void WriteOneCounter_OneMetricIsFlushed()
    {
      _statsd.AddAggregator(MessageType.Counter, AggregatorFactory.CreateTimedCountersBlock("", new TimeSpan(0, 0, 0, 0, 100)));
      var stat = StatsBuilder.Counter.foo.bar.baz + 1;
      _listener.Send(stat);
      _listener.Send(stat);
      Thread.Sleep(500);
      Assert.IsNotNull(_backend.LastMessage);
      Assert.AreEqual(5, _backend.LastMessage[0].Count);
    }
  }
}
