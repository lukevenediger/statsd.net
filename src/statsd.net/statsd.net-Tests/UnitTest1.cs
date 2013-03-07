using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks.Dataflow;
using statsd.net;
using statsd.net_Tests.Infrastructure;
using statsd.net.Messages;
using statsd.net.System;
using statsd.net.Backends;

namespace statsd.net_Tests
{
  [TestClass]
  public class UnitTest1
  {
    [TestMethod]
    public void TestMethod1()
    {
      var mock = new Mock<ITargetBlock<string>>();
      var statsd = new Statsd();
      var listener = new InAppListener();
      statsd.AddListener(listener);
      statsd.AddAggregator(MessageType.Counter, AggregatorFactory.CreateTimedCountersBlock("stats_counts", new TimeSpan(0, 0, 1)));
      statsd.AddBackend(new ConsoleBackend());
      listener.Send("foo.bar.baz:1|c|");
      Console.ReadLine();
    }
  }
}
