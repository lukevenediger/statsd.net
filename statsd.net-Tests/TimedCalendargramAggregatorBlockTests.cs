using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using statsd.net.Framework;
using statsd.net.shared.Messages;
using statsd.net.shared.Structures;
using statsd.net_Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net_Tests
{
    [TestClass]
    public class TimedCalendargramAggregatorBlockTests
    {
        private ActionBlock<StatsdMessage> _block;
        private ControllableIntervalService _intervalService;
        private BucketOutputBlock _outputBuffer;
        private Mock<ILog> _log;
        private MockTimeWindowService _timeWindowService;
        private DateTime _startDateTime;

        [TestInitialize]
        public void Initialise()
        {
            _intervalService = new ControllableIntervalService();
            _outputBuffer = new BucketOutputBlock();
            _startDateTime = new DateTime(2012, 04, 05, 17, 05, 10);
            _timeWindowService = new MockTimeWindowService(_startDateTime);
            _log = new Mock<ILog>();
            _block = TimedCalendargramAggregatorBlockFactory.CreateBlock(_outputBuffer,
                String.Empty,
                _intervalService,
                _timeWindowService,
                _log.Object);
        }

        [TestMethod]
        public void LogOneSet_OneGraphiteLine_NoTimeChange_NoOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "h"));
            _block.WaitUntilAllItemsProcessed();
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(0, _outputBuffer.Items.Count);
        }

        [TestMethod]
        public void LogOneSet_OneGraphiteLine_TimeChange_HasOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "h"));
            _block.WaitUntilAllItemsProcessed();
            _timeWindowService.AddHours(1);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(1, _outputBuffer.Items.Count);
            Assert.AreEqual(1, _outputBuffer["foo.set.hour." + _startDateTime.Hour]);
        }

        [TestMethod]
        public void LogOneSet_TwoOfTheSameGraphiteLines_TimeChange_HasOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "h"));
            _block.Post(new Calendargram("foo.set", "baz", "h"));
            _block.WaitUntilAllItemsProcessed();
            _timeWindowService.AddHours(1);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(1, _outputBuffer.Items.Count);
            Assert.AreEqual(1, _outputBuffer["foo.set.hour." + _startDateTime.Hour]);
        }

        [TestMethod]
        public void LogOneSet_TwoDifferentGraphiteLines_TimeChange_HasOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "h"));
            _block.Post(new Calendargram("foo.set", "map", "h"));
            _block.WaitUntilAllItemsProcessed();
            _timeWindowService.AddHours(1);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(1, _outputBuffer.Items.Count);
            Assert.AreEqual(2, _outputBuffer["foo.set.hour." + _startDateTime.Hour]);
        }

        [TestMethod]
        public void LogOneSet_TwoDifferentHours_TimeChange_PulseInCurrentHour_HasOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "h"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddHours(1);
            _block.Post(new Calendargram("foo.set", "map", "h"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(1, _outputBuffer.Items.Count);
            Assert.AreEqual(1, _outputBuffer["foo.set.hour." + _startDateTime.Hour]);
        }

        [TestMethod]
        public void LogOneSet_TwoDifferentHours_TimeChange_PulseInNextHour_HasOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "h"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddHours(1);
            _block.Post(new Calendargram("foo.set", "map", "h"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddHours(1);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(2, _outputBuffer.Items.Count);
            Assert.AreEqual(1, _outputBuffer["foo.set.hour." + _startDateTime.Hour]);
            Assert.AreEqual(1, _outputBuffer["foo.set.hour." + (_startDateTime.Hour + 1)]);
        }

        [TestMethod]
        public void LogOneSet_TwoDifferentDays_TimeChange_PulseInNextDay_HasOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "d"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddDays(1);
            _block.Post(new Calendargram("foo.set", "map", "d"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddDays(1);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(2, _outputBuffer.Items.Count);
            Assert.AreEqual(1, _outputBuffer["foo.set.day." + _startDateTime.Day]);
            Assert.AreEqual(1, _outputBuffer["foo.set.day." + (_startDateTime.Day + 1)]);
        }

        [TestMethod]
        public void LogOneSet_TwoDifferentDaysOfWeek_TimeChange_PulseInNextDayOfWeek_HasOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "dow"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddDays(1);
            _block.Post(new Calendargram("foo.set", "map", "dow"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddDays(1);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(2, _outputBuffer.Items.Count);
            Assert.AreEqual(1, _outputBuffer["foo.set." + TimeWindow.WEEKDAY + "." + _startDateTime.DayOfWeek]);
            Assert.AreEqual(1, _outputBuffer["foo.set." + TimeWindow.WEEKDAY + "." + _startDateTime.AddDays(1).DayOfWeek]);
        }

        [TestMethod]
        public void LogOneSet_TwoDifferentWeeks_TimeChange_PulseInNextWeek_HasOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "w"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddDays(7);
            _block.Post(new Calendargram("foo.set", "map", "w"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddDays(7);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(2, _outputBuffer.Items.Count);
            Assert.AreEqual(1, _outputBuffer["foo.set." + TimeWindow.WEEK + "." + _startDateTime.GetIso8601WeekOfYear()]);
            Assert.AreEqual(1, _outputBuffer["foo.set." + TimeWindow.WEEK + "." + _startDateTime.AddDays(7).GetIso8601WeekOfYear()]);
        }

        [TestMethod]
        public void LogOneSet_TwoDifferentMonths_TimeChange_PulseInNextMonth_HasOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "m"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddMonths(1);
            _block.Post(new Calendargram("foo.set", "map", "m"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddMonths(1);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(2, _outputBuffer.Items.Count);
            Assert.AreEqual(1, _outputBuffer["foo.set." + TimeWindow.MONTH + "." + _startDateTime.Month]);
            Assert.AreEqual(1, _outputBuffer["foo.set." + TimeWindow.MONTH + "." + (_startDateTime.Month + 1)]);
        }

        [TestMethod]
        public void LogOneSet_TwoDifferentFiveMinutes_TimeChange_PulseInNextFiveMinutes_HasOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "5min"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddMinutes(5);
            _block.Post(new Calendargram("foo.set", "map", "5min"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddMinutes(5);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(2, _outputBuffer.Items.Count);
            Assert.AreEqual(1, _outputBuffer["foo.set." + TimeWindow.FIVE_MINUTE + ".1"]);
            Assert.AreEqual(1, _outputBuffer["foo.set." + TimeWindow.FIVE_MINUTE + ".2"]);
        }

        [TestMethod]
        public void LogOneSet_TwoDifferentOneMinutes_TimeChange_PulseInNextOneMinute_HasOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "1min"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddMinutes(1);
            _block.Post(new Calendargram("foo.set", "map", "1min"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _timeWindowService.AddMinutes(1);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(2, _outputBuffer.Items.Count);
            Assert.AreEqual(1, _outputBuffer["foo.set." + TimeWindow.ONE_MINUTE + ".5"]);
            Assert.AreEqual(1, _outputBuffer["foo.set." + TimeWindow.ONE_MINUTE + ".6"]);
        }

        [TestMethod]
        public void LogOneSet_SameMinute_NoTimeChange_PulseInSameMinute_HasNoOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "1min"));
            _block.Post(new Calendargram("foo.set", "map", "1min"));
            _block.WaitUntilAllItemsProcessed();
            Thread.Sleep(500);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(0, _outputBuffer.Items.Count);
        }

        [TestMethod]
        public void LogOneSet_OneGraphiteLine_IrrelevantTimeChange_NoOutput_Success()
        {
            _block.Post(new Calendargram("foo.set", "baz", "d"));
            _block.WaitUntilAllItemsProcessed();
            _timeWindowService.AddHours(1);
            _intervalService.Pulse();
            _block.CompleteAndWait();

            Assert.AreEqual(0, _outputBuffer.Items.Count);
        }
    }
}
