using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineStatsClient
{
  class Options
  {
    [Option('h', "host", Required = false, DefaultValue = "localhost", HelpText="Statsd host name")]
    public string Host { get; set; }
    [Option('p', "port", Required = false, DefaultValue = 12000, HelpText="Statsd listen port")]
    public int Port { get; set; }
    [Option('c', "count", MutuallyExclusiveSet = "count", Required = false, HelpText="Log a count")]
    public bool Count { get; set; }
    [Option('t', "timing", MutuallyExclusiveSet = "timing", Required = false, HelpText="Log a timing")]
    public bool Timing { get; set; }
    [Option('g', "gauge", MutuallyExclusiveSet = "gauge", Required = false, HelpText="Log a gauge")]
    public bool Gauge { get; set; }
    [Option('n', "name", Required = true, HelpText = "The name of the stat to be logged.")]
    public string Name { get; set;}
    [Option('v', "value", Required = false, DefaultValue = 1, HelpText = "The value to log")]
    public int Value { get; set; }

    [HelpOption]
    public string GetUsage()
    {
      return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
    }
  }
}
