using Microsoft.VisualStudio.TestTools.UnitTesting;
using statsd.net.Framework;
using statsd.net.Messages;
using statsd.net_Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net;

namespace statsd.net_Tests
{
  [TestClass]
  public class TimedGaugeAggregatorBlockTests
  {
    private ActionBlock<StatsdMessage> _block;
    private ControllableIntervalService _intervalService;
    private GraphiteLineOutputBlock _outputBuffer;
    
    [TestInitialize]
    public void Initialise()
    {
      _intervalService = new ControllableIntervalService();
      _outputBuffer = new GraphiteLineOutputBlock();
      _block = TimedGaugeAggregatorBlockFactory.CreateBlock(_outputBuffer,
        String.Empty,
        _intervalService);
    }

    [TestMethod]
    public void LogOneGauge_OneGraphiteLine_Success()
    {
      _block.Post(new Gauge("foo", 1));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();
      _block.CompleteAndWait();

      Assert.AreEqual(1, _outputBuffer.Items.Count);
      Assert.AreEqual(1, _outputBuffer["foo"]);
    }

    [TestMethod]
    public void LogMultipleValuesForOneGauge_OneGraphiteLine_Success()
    {
      TestUtility.Range(100, false).ForEach(p => _block.Post(new Gauge("foo", p)));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();
      _block.CompleteAndWait();

      Assert.AreEqual(1, _outputBuffer.Items.Count);
      Assert.AreEqual(100, _outputBuffer["foo"]);
    }

    [TestMethod]
    public void LogTwoDifferentGauges_TwoGraphiteLines_Success()
    {
      _block.Post(new Gauge("foo", 1));
      _block.Post(new Gauge("bar", 1));
      _block.WaitUntilAllItemsProcessed();
      _intervalService.Pulse();
      _block.CompleteAndWait();

      Assert.AreEqual(2, _outputBuffer.Items.Count);
      Assert.AreEqual(1, _outputBuffer["foo"]);
      Assert.AreEqual(1, _outputBuffer["bar"]);
    }
  }
}
