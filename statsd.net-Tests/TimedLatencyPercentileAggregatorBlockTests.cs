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
  public class TimedLatencyPercentileAggregatorBlockTests
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
    }

    [TestMethod]
    public void WriteOneHunderedLatencies_p50PercentileLogged_Success()
    {
      _block = TimedLatencyPercentileAggregatorBlockFactory.CreateBlock(_outputBuffer,
        String.Empty,
        _intervalService,
        50,
        null,
        _log.Object);

      TestUtility.Range(100, false).ForEach(p => _block.Post(new Timing("foo", p)));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();

      Assert.AreEqual(50, _outputBuffer["foo.p50"]);
    }

    [TestMethod]
    public void WriteOneHunderedLatencies_p90PercentileLogged_Success()
    {
      _block = TimedLatencyPercentileAggregatorBlockFactory.CreateBlock(_outputBuffer,
        String.Empty,
        _intervalService,
        90,
        null,
        _log.Object);

      TestUtility.Range(100, false).ForEach(p => _block.Post(new Timing("foo", p)));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();

      Assert.AreEqual(90, _outputBuffer["foo.p90"]);
    }

    [TestMethod]
    public void WriteFourLatencies_PercentileLogged_Success()
    {
      _block = TimedLatencyPercentileAggregatorBlockFactory.CreateBlock(_outputBuffer,
        String.Empty,
        _intervalService,
        90,
        null,
        _log.Object);

      _block.Post(new Timing("foo", 100));
      _block.Post(new Timing("foo", 200));
      _block.Post(new Timing("foo", 300));
      _block.Post(new Timing("foo", 400));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();

      Assert.IsTrue(_outputBuffer.GraphiteLines.Any(p => p.Name == "foo.p90"));
      Assert.AreEqual(400, _outputBuffer["foo.p90"]);
    }

    [TestMethod]
    public void WriteLatenciesToTwoBuckets_MeasurementsSeparate_Success()
    {
      _block = TimedLatencyPercentileAggregatorBlockFactory.CreateBlock(_outputBuffer,
        String.Empty,
        _intervalService,
        80,
        null,
        _log.Object);
      var pulseDate = DateTime.Now;

      // Bucket one
      TestUtility.Range(5, false).ForEach(p => _block.Post(new Timing("foo", p * 100)));
      // Bucket two
      TestUtility.Range(5, false).ForEach(p => _block.Post(new Timing("bar", p * 100)));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse(pulseDate);

      Assert.AreEqual(400, _outputBuffer["foo.p80"]);
      Assert.AreEqual(400, _outputBuffer["bar.p80"]);
      Assert.AreEqual(2, _outputBuffer.Items.Count);
    }
  }
}
