using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StatsdClient;

namespace DemoDataFeeder
{
  class Program
  {
    static void Main(string[] args)
    {
      var options = new Options();
      if (CommandLine.Parser.Default.ParseArgumentsStrict(args, options))
      {
        var client = new StatsdClient.StatsdClient(options.Host, options.Port);
        var tokenSource = new System.Threading.CancellationTokenSource();
        var task = Task.Factory.StartNew(() =>
          {
            var rnd = new Random();
            Console.WriteLine("Feeding stats to {0}:{1}, ctrl+c to exit.", options.Host, options.Port);
            while (true)
            {
              client.LogCount("test.count.one." + rnd.Next(5));
              client.LogCount("test.count.bigValue", rnd.Next(50));
              client.LogTiming("test.timing." + rnd.Next(5), rnd.Next(100, 1000));
              client.LogGauge("test.gauge." + rnd.Next(5), rnd.Next(100));
              Thread.Sleep(rnd.Next(50, 300));
            }
          },
          tokenSource.Token);
        Console.CancelKeyPress += (sender, e) =>
          {
            tokenSource.Cancel();
          };
        task.Wait();
      }
    }
  }
}
