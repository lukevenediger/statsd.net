using statsd.net.Listeners;
using statsd.net.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.ServiceConfigurators;

namespace statsd.net
{
  class Program
  {
    static void Main(string[] args)
    {
     HostFactory.Run(x =>
       {
         x.Service(p => new StatsdService());
         x.RunAsLocalService();
         x.SetDisplayName("Statsd.net");
         x.SetDescription("A stats aggregation service for Graphite.");
         x.SetServiceName("Statsd.net");
         x.StartAutomatically();
       });
    }
  }
}
