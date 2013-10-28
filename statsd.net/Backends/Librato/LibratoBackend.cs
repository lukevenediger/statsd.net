using log4net;
using Microsoft.Practices.TransientFaultHandling;
using RestSharp;
using statsd.net.Configuration;
using statsd.net.shared;
using statsd.net.shared.Backends;
using statsd.net.shared.Messages;
using statsd.net.shared.Services;
using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
    public const string LIBRATO_API_URL = "http://metrics-api.librato.com";

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
    private RetryPolicy<LibratoErrorDetectionStrategy> _retryPolicy;
    private Incremental _retryStrategy;
    private LibratoBackendConfiguration _config;

    public int OutputCount
    {
      get { return _pendingOutputCount; }
    }

    public LibratoBackend(LibratoBackendConfiguration configuration, string source, ISystemMetricsService systemMetrics)
    {
      _completionTask = new Task(() => IsActive = false);
      _log = SuperCheapIOC.Resolve<ILog>();
      _systemMetrics = systemMetrics;
      _config = configuration;
      _config.Source = source;
      _serviceVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
      
      _preprocessorBlock = new ActionBlock<Bucket>(bucket => ProcessBucket(bucket), Utility.UnboundedExecution());
      _batchBlock = new BatchBlock<LibratoMetric>(_config.MaxBatchSize);
      _outputBlock = new ActionBlock<LibratoMetric[]>(lines => PostToLibrato(lines), Utility.OneAtATimeExecution());
      _batchBlock.LinkTo(_outputBlock);

      _client = new RestClient(LIBRATO_API_URL);
      _client.Authenticator = new HttpBasicAuthenticator(_config.Email, _config.Token);
      _client.Timeout = (int)_config.PostTimeout.TotalMilliseconds;

      _retryPolicy = new RetryPolicy<LibratoErrorDetectionStrategy>(_config.NumRetries);
      _retryPolicy.Retrying += (sender, args) =>
        {
          _log.Warn(String.Format("Retry {0} failed. Trying again. Delay {1}, Error: {2}", args.CurrentRetryCount, args.Delay, args.LastException.Message), args.LastException);
          _systemMetrics.LogCount("backends.librato.retry");
        };
      _retryStrategy = new Incremental(_config.NumRetries, _config.RetryDelay, TimeSpan.FromSeconds(2));
      IsActive = true;
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

    private void ProcessBucket(Bucket bucket)
    {
      switch (bucket.BucketType)
      {
        case BucketType.Count:
          var counterBucket = bucket as CounterBucket;
          foreach (var count in counterBucket.Items)
          {
            if (_config.CountersAsGauges)
            {
              _batchBlock.Post(new LibratoGauge(counterBucket.RootNamespace + count.Key, count.Value, bucket.Epoch));
            }
            else
            {
              _batchBlock.Post(new LibratoCounter(counterBucket.RootNamespace + count.Key, count.Value, bucket.Epoch));
            }
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
        var payload = GetPayload(epochGroup);
        pendingLines = payload.gauges.Length + payload.counters.Length;
        _systemMetrics.LogGauge("backends.librato.lines", pendingLines);
        Interlocked.Add(ref _pendingOutputCount, pendingLines);

        var request = new RestRequest("/v1/metrics", Method.POST);
        request.RequestFormat = DataFormat.Json;
        request.AddHeader("User-Agent", "statsd.net-librato-backend/" + _serviceVersion);
        request.AddBody(payload);

        _retryPolicy.ExecuteAction(() =>
          {
            bool succeeded = false;
            try
            {
              _systemMetrics.LogCount("backends.librato.post.attempt");
              var result = _client.Execute(request);
              if (result.StatusCode == HttpStatusCode.Unauthorized)
              {
                _systemMetrics.LogCount("backends.librato.error.unauthorised");
                throw new UnauthorizedAccessException("Librato.com reports that your access is not authorised. Is your API key and email address correct?");
              }
              else if (result.StatusCode != HttpStatusCode.OK)
              {
                _systemMetrics.LogCount("backends.librato.error." + result.StatusCode.ToString());
                throw new Exception(String.Format("Request could not be processed. Server said {0}", result.StatusCode.ToString()));
              }
              else
              {
                succeeded = true;
                _log.Info(String.Format("Wrote {0} lines to Librato.", pendingLines));
              }
            }
            finally
            {
              Interlocked.Add(ref _pendingOutputCount, -pendingLines);
              _systemMetrics.LogCount("backends.librato.post." + (succeeded ? "success" : "failure"));
            }
          });
      }
    }

    private APIPayload GetPayload(IGrouping<long, LibratoMetric> epochGroup)
    {
      var lines = epochGroup.ToList();
      // Split the lines up into gauges and counters
      var gauges = lines.Where(p => p.MetricType == LibratoMetricType.Gauge || p.MetricType == LibratoMetricType.Timing).ToArray();
      var counts = lines.Where(p => p.MetricType == LibratoMetricType.Counter).ToArray();

      var payload = new APIPayload();
      payload.gauges = gauges;
      payload.counters = counts;
      payload.measure_time = epochGroup.Key;
      payload.source = _config.Source;
      return payload;
    }

    private class LibratoErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
      public bool IsTransient(Exception ex)
      {
        if (ex is TimeoutException)
        {
          return true;
        }
        return false;
      }
    }
  }
}
