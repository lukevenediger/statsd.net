using Microsoft.VisualStudio.TestTools.UnitTesting;
using statsd.net;
using statsd.net_Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net_Tests
{
  [TestClass]
  public abstract class StatsdTestSuite
  {
    protected Statsd _statsd;
    protected InAppListener _listener;
    protected InAppBackend _backend;

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
  }
}
