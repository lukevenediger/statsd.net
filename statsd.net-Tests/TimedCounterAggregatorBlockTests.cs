using Microsoft.VisualStudio.TestTools.UnitTesting;
using statsd.net.Framework;
using statsd.net.shared.Messages;
using statsd.net_Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net;
using log4net;
using Moq;

namespace statsd.net_Tests
{
  [TestClass]
  public class TimedCounterAggregatorBlockTests
  {
    private ActionBlock<StatsdMessage> _block;
    private ControllableIntervalService _intervalService;
    private BucketOutputBlock _outputBuffer;
    private Mock<ILog> _log;

    [TestInitialize]
    public void Initialise()
    {
      _intervalService = new ControllableIntervalService();
      _outputBuffer = new BucketOutputBlock();
      _log = new Mock<ILog>();
      _block = TimedCounterAggregatorBlockFactory.CreateBlock(_outputBuffer,
        String.Empty,
        _intervalService,
        _log.Object);
    }

    [TestMethod]
    public void LogOneCount_OneGraphiteLine_Success()
    {
      _block.Post(new Counter("foo", 1));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();
      _block.CompleteAndWait();

      Assert.AreEqual(1, _outputBuffer.Items.Count);
      Assert.AreEqual(1, _outputBuffer["foo"]);
    }

    [TestMethod]
    public void LogOneHunderedCounts_OneGraphiteLine_Success()
    {
      _block.Post(new Counter("foo", 1));
      TestUtility.Range(100, false).ForEach(p => _block.Post(new Counter("foo", p)));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();
      _block.CompleteAndWait();

      Assert.AreEqual(1, _outputBuffer.Items.Count);
      Assert.AreEqual(5051, _outputBuffer["foo"]);
    }

    [TestMethod]
    public void LogTwoSeparateMetrics_TwoGraphiteLines_Success()
    {
      _block.Post(new Counter("foo", 1));
      _block.Post(new Counter("bar", 1));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();
      _block.CompleteAndWait();

      Assert.AreEqual(2, _outputBuffer.Items.Count);
      Assert.AreEqual(1, _outputBuffer["foo"]);
      Assert.AreEqual(1, _outputBuffer["bar"]);
    }
  }
}
