using log4net;
using RestSharp;
using statsd.net.shared;
using statsd.net.shared.Backends;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.Backends.Librato
{
  /**
   * Flow of data:
   *  
   *   Bucket ->
   *      Preprocessor ->
   *         Batch Block ->
   *            Post to Librato 
   */
  public class LibratoBackend : IBackend
  {
    public const string ILLEGAL_NAME_CHARACTERS = @"[^-.:_\w]+";

    private Task _completionTask;
    private ILog _log;
    private string _serviceVersion;
    public bool IsActive { get; private set; }
    private ActionBlock<Bucket> _preprocessorBlock;
    private BatchBlock<LibratoMetric> _batchBlock;
    private ActionBlock<LibratoMetric[]> _outputBlock;
    private RestClient _client;
    private ISystemMetricsService _systemMetrics;
    private int _pendingOutputCount;

    private LibratoConfig _config;

    public int OutputCount
    {
      get { return _pendingOutputCount; }
    }

    public LibratoBackend(dynamic configuration, ISystemMetricsService systemMetrics)
    {
      _completionTask = new Task(() => IsActive = false);
      _log = SuperCheapIOC.Resolve<ILog>();
      _systemMetrics = systemMetrics;
      _config = GetConfiguration(configuration);
      _serviceVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
      
      _preprocessorBlock = new ActionBlock<Bucket>(bucket => ProcessBucket(bucket), Utility.UnboundedExecution());
      _batchBlock = new BatchBlock<LibratoMetric>(10); //_config.MaxBatchSize);
      _outputBlock = new ActionBlock<LibratoMetric[]>(lines => PostToLibrato(lines), Utility.OneAtATimeExecution());
      _batchBlock.LinkTo(_outputBlock);

      _client = new RestClient(_config.Api);
      _client.Authenticator = new HttpBasicAuthenticator(_config.Email, _config.Token);
      _client.Timeout = _config.PostTimeoutSeconds * 1000;
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader,
      Bucket messageValue,
      ISourceBlock<Bucket> source,
      bool consumeToAccept)
    {
      _preprocessorBlock.Post(messageValue);
      return DataflowMessageStatus.Accepted;
    }

    public void Complete()
    {
      _completionTask.Start();
    }

    public Task Completion
    {
      get { return _completionTask; }
    }

    public void Fault(Exception exception)
    {
      throw new NotImplementedException();
    }

    private LibratoConfig GetConfiguration(dynamic configuration)
    {
      dynamic configWrapped = new BetterExpando(ignoreCase: true,
        returnEmptyStringForMissingProperties: true,
        root: configuration);
      var config = new LibratoConfig();
      config.Email = configWrapped.email;
      config.Token = configWrapped.token;
      config.Source = configWrapped.source;
      config.SkipInternalMetrics = configWrapped.ValueOrDefault("SkipInternalMetrics", false);
      config.RetryDelaySeconds = (int)configWrapped.ValueOrDefault("RetryDelaySeconds", (long)5);
      config.PostTimeoutSeconds = (int)configWrapped.ValueOrDefault("PostTimeoutSeconds", (long)4);
      config.MaxBatchSize = (int)configWrapped.ValueOrDefault("MaxBatchSize", (long)500);
      config.Api = configWrapped.ValueOrDefault("Api", "https://metrics-api.librato.com");
      return config;
    }

    private void ProcessBucket(Bucket bucket)
    {
      switch (bucket.BucketType)
      {
        case BucketType.Count:
          var counterBucket = bucket as CounterBucket;
          foreach (var count in counterBucket.Items)
          {
            _batchBlock.Post(new LibratoCounter(counterBucket.RootNamespace + count.Key, count.Value, bucket.Epoch));
          }
          break;
        case BucketType.Gauge:
          var gaugeBucket = bucket as GaugesBucket;
          foreach (var gauge in gaugeBucket.Gauges)
          {
            _batchBlock.Post(new LibratoGauge(gaugeBucket.RootNamespace + gauge.Key, gauge.Value, bucket.Epoch));
          }
          break;
        case BucketType.Timing:
          var timingBucket = bucket as LatencyBucket;
          foreach (var timing in timingBucket.Latencies)
          {
            _batchBlock.Post(new LibratoTiming(timingBucket.RootNamespace + timing.Key,
              timing.Value.Count,
              timing.Value.Sum,
              timing.Value.SumSquares,
              timing.Value.Min,
              timing.Value.Max,
              bucket.Epoch));
          }
          break;
        case BucketType.Percentile:
          var percentileBucket = bucket as PercentileBucket;
          int percentileValue;
          foreach (var pair in percentileBucket.Timings)
          {
            if (percentileBucket.TryComputePercentile(pair, out percentileValue))
            {
              _batchBlock.Post(new LibratoGauge(percentileBucket.RootNamespace + pair.Key + percentileBucket.PercentileName,
                percentileValue,
                bucket.Epoch));
            }
          }
          break;
      }
    }

    private void PostToLibrato(LibratoMetric[] lines)
    {
      var pendingLines = 0;
      foreach (var epochGroup in lines.GroupBy(p => p.Epoch))
      {
        dynamic payload = GetPayload(epochGroup);
        pendingLines = payload.gauges.Length + payload.counters.Length;
        Interlocked.Add(ref _pendingOutputCount, pendingLines);
        
        var request = new RestRequest("/v1/metrics", Method.POST);
        request.RequestFormat = DataFormat.Json;
        request.AddHeader("User-Agent", "statsd.net-librato-backend/" + _serviceVersion);
        request.AddBody(payload);
        var result = _client.Execute(request);

        Interlocked.Add(ref _pendingOutputCount, -pendingLines);
      }
    }

    private dynamic GetPayload(IGrouping<long, LibratoMetric> epochGroup)
    {
      var lines = epochGroup.ToList();
      // Split the lines up into gauges and counters
      var gauges = lines.Where(p => p.MetricType == LibratoMetricType.Gauge || p.MetricType == LibratoMetricType.Timing ).ToArray();
      var counts = lines.Where(p => p.MetricType == LibratoMetricType.Counter).ToArray();

      dynamic payload;
      if (String.IsNullOrEmpty(_config.Source))
      {
        payload = new
        {
          gauges = gauges,
          counters = counts,
          measure_time = epochGroup.Key
        };
      }
      else
      {
        payload = new
        {
          gauges = gauges,
          counters = counts,
          measure_time = epochGroup.Key,
          source = _config.Source
        };
      }
      return payload;
    }
  }
}
