using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        var client = new Statsd(options.Host, options.Port, prefix : options.Namespace, connectionType : options.UseTCP ? ConnectionType.Tcp : ConnectionType.Udp);
        var tokenSource = new System.Threading.CancellationTokenSource();
        var stopwatch = Stopwatch.StartNew();
        var totalMetricsSent = 0;
        var tasks = new List<Task>();
        int numThreads = options.Threads == 0 ? 1 : options.Threads;
        for ( int count = 0; count < numThreads; count++ )
        {
          int myTaskNumber = count;
          var task = Task.Factory.StartNew( () =>
            {
              var rnd = new Random();
              int taskNumber = myTaskNumber;
              if ( taskNumber == 0 )
              {
                Console.WriteLine( "Feeding stats to {0}:{1}, ctrl+c to exit.", options.Host, options.Port );
              }
              while ( true )
              {
                client.LogCount( "test.count.one." + rnd.Next( 5 ) );
                client.LogCount( "test.count.bigValue", rnd.Next( 50 ) );
                client.LogTiming( "test.timing." + rnd.Next( 5 ), rnd.Next( 100, 2000 ) );
                client.LogGauge( "test.gauge." + rnd.Next( 5 ), rnd.Next( 100 ) );
                Thread.Sleep( options.Delay );
                Interlocked.Add( ref totalMetricsSent, 4 );

                if ( taskNumber == 0 && stopwatch.ElapsedMilliseconds >= 5000 )
                {
                  Console.WriteLine( "Total sent: {0}", totalMetricsSent );
                  stopwatch.Restart();
                }
              }
            },
            tokenSource.Token );
          tasks.Add( task );
        }
        Console.CancelKeyPress += (sender, e) =>
          {
            tokenSource.Cancel();
          };
        Task.WaitAll( tasks.ToArray() );
      }
    }
  }
}
