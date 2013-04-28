using CommandLine;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineStatsClient
{
  class Program
  {
    /*
     * Usage: statsd -c foo.bar.baz.zom 
    */
    static void Main(string[] args)
    {
      var options = new Options();
      if (Parser.Default.ParseArgumentsStrict(args, options))
      {
        var client = new Statsd(options.Host, options.Port, rethrowOnError : true);
        if (options.Count)
        {
          client.LogCount(options.Name, options.Value);
        }
        else if (options.Timing)
        {
          client.LogTiming(options.Name, options.Value);
        }
        else if (options.Gauge)
        {
          client.LogGauge(options.Name, options.Value);
        }
        else
        {
          Console.WriteLine("No metric type specified.");
        }
      }
    }
  }
}
