using Microsoft.VisualStudio.TestTools.UnitTesting;
using statsd.net;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net.Framework;
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
  public class SystemTests : StatsdTestSuite
  {
    [TestMethod]
    [Timeout(10 * 1000)] /* 10 seconds */
    public void SystemStartsWithNoTraffic_ShutsDown()
    {
      _statsd.Stop();
    }
  }
}
