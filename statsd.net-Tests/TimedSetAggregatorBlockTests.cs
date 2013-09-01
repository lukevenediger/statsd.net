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
  public class TimedSetAggregatorBlockTests
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
      _block = TimedSetAggregatorBlockFactory.CreateBlock(_outputBuffer,
        String.Empty,
        _intervalService,
        _log.Object);
    }

    [TestMethod]
    public void WriteOneSet_OneGraphiteLine_Success()
    {
      _block.Post(new Set("foo", 10));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();
      _block.CompleteAndWait();

      Assert.AreEqual(1, _outputBuffer.Items.Count);
      Assert.AreEqual(1, _outputBuffer["foo"]);
    }

    [TestMethod]
    public void Write100EntriesToOneSet_OneGraphiteLine_Success()
    {
      TestUtility.Range(100).ForEach(
        p => _block.Post(new Set("foo", p * 100)));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();
      _block.CompleteAndWait();

      Assert.AreEqual(1, _outputBuffer.Items.Count());
      Assert.AreEqual(100, _outputBuffer["foo"]);
    }

    [TestMethod]
    public void WriteOneEntryToOneHunderedSets_OneGraphiteLine_Success()
    {
      TestUtility.Range(100).ForEach(
        p => _block.Post(new Set("foo" + p, p)));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();
      _block.CompleteAndWait();

      Assert.AreEqual(100, _outputBuffer.Items.Count());
      Assert.AreEqual(1, _outputBuffer["foo1"]);
    }
  }
}
