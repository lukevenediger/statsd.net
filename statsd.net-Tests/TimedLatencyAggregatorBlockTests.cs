using Microsoft.VisualStudio.TestTools.UnitTesting;
using statsd.net.Framework;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net_Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net;
using System.Threading;
using log4net;
using Moq;

namespace statsd.net_Tests
{
  [TestClass]
  public class TimedLatencyAggregatorBlockTests
  {
    private ActionBlock<StatsdMessage> _block;
    private ControllableIntervalService _intervalService;
    private GraphiteLineOutputBlock _outputBuffer;
    private Mock<ILog> _log;

    [TestInitialize]
    public void Initialise()
    {
      _intervalService = new ControllableIntervalService();
      _outputBuffer = new GraphiteLineOutputBlock();
      _log = new Mock<ILog>();
    }

    [TestMethod]
    public void WriteOneLatency_OneMetricFlushed_Success()
    {
      _block = TimedLatencyAggregatorBlockFactory.CreateBlock(_outputBuffer,
        String.Empty,
        _intervalService,
        _log.Object);
      var pulseDate = DateTime.Now;

      _block.Post(new Timing("foo.bar.baz", 100));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse(pulseDate);

      Assert.AreEqual(new GraphiteLine("foo.bar.baz.count", 1, pulseDate.ToEpoch()), _outputBuffer[0]);
      Assert.AreEqual(new GraphiteLine("foo.bar.baz.min", 100, pulseDate.ToEpoch()), _outputBuffer[1]);
      Assert.AreEqual(new GraphiteLine("foo.bar.baz.max", 100, pulseDate.ToEpoch()), _outputBuffer[2]);
      Assert.AreEqual(new GraphiteLine("foo.bar.baz.mean", 100, pulseDate.ToEpoch()), _outputBuffer[3]);
      Assert.AreEqual(new GraphiteLine("foo.bar.baz.sum", 100, pulseDate.ToEpoch()), _outputBuffer[4]);
      Assert.AreEqual(5, _outputBuffer.Items.Count);
      // Ensure that min, max, mean and sum are all equal
      Assert.IsTrue(_outputBuffer[1].Quantity == _outputBuffer[2].Quantity 
        && _outputBuffer[2].Quantity == _outputBuffer[3].Quantity
        && _outputBuffer[3].Quantity == _outputBuffer[4].Quantity);
    }

    [TestMethod]
    public void WriteTwoLatencies_CalulateMinMaxMeanSum_Success()
    {
      _block = TimedLatencyAggregatorBlockFactory.CreateBlock(_outputBuffer,
        String.Empty,
        _intervalService,
        _log.Object);
      var pulseDate = DateTime.Now;

      _block.Post(new Timing("foo", 5));
      _block.Post(new Timing("foo", 15));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse(pulseDate);

      Assert.AreEqual(15, _outputBuffer["foo.max"]);
      Assert.AreEqual(5, _outputBuffer["foo.min"]);
      Assert.AreEqual(10, _outputBuffer["foo.mean"]);
      Assert.AreEqual(2, _outputBuffer["foo.count"]);
      Assert.AreEqual(20, _outputBuffer["foo.sum"]);
    }

    [TestMethod]
    public void WriteLatenciesToTwoBuckets_MeasurementsSeparate_Success()
    {
      _block = TimedLatencyAggregatorBlockFactory.CreateBlock(_outputBuffer,
        String.Empty,
        _intervalService,
        _log.Object);
      var pulseDate = DateTime.Now;

      // Bucket one
      TestUtility.Range(5, false).ForEach(p => _block.Post(new Timing("foo", p * 100)));
      // Bucket two
      TestUtility.Range(5, false).ForEach(p => _block.Post(new Timing("bar", p * 100)));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse(pulseDate);

      Assert.AreEqual(5, _outputBuffer["foo.count"]);
      Assert.AreEqual(100, _outputBuffer["foo.min"]);
      Assert.AreEqual(500, _outputBuffer["foo.max"]);
      Assert.AreEqual(300, _outputBuffer["foo.mean"]);
      Assert.AreEqual(1500, _outputBuffer["foo.sum"]);

      Assert.AreEqual(5, _outputBuffer["bar.count"]);
      Assert.AreEqual(100, _outputBuffer["bar.min"]);
      Assert.AreEqual(500, _outputBuffer["bar.max"]);
      Assert.AreEqual(300, _outputBuffer["bar.mean"]);
      Assert.AreEqual(1500, _outputBuffer["bar.sum"]);

      Assert.AreEqual(10, _outputBuffer.Items.Count);
    }
  }
}
