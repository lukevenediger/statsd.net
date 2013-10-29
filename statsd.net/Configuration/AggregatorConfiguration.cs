using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Configuration
{
  public class AggregatorConfiguration
  {
  }  public class GaugeAggregatorConfig : AggregatorConfiguration
  {
    public string Namespace { get; set; }
    public bool RemoveZeroGauges { get; set; }

    public GaugeAggregatorConfig(string ns, bool removeZeroGauges)
    {
      Namespace = ns;
      RemoveZeroGauges = removeZeroGauges;
    }
  }

  public class CounterAggregationConfig : AggregatorConfiguration
  {
    public string Namespace { get; set; }

    public CounterAggregationConfig(string ns)
    {
      // TODO: Complete member initialization
      Namespace = ns;
    }
  }

  public class SetAggregationConfig : AggregatorConfiguration
  {
    public string Namespace { get; set; }

    public SetAggregationConfig(string ns)
    {
      Namespace = ns;
    }
  }

  public class TimersAggregationConfig : AggregatorConfiguration
  {
    private List<PercentileConfig> _percentiles;

    public string Namespace { get; set; }
    public bool CalculateSumSquares { get; set; }
    public IReadOnlyList<PercentileConfig> Percentiles { get { return _percentiles; } }

    public TimersAggregationConfig(string ns, bool calculateSumSquares)
    {
      Namespace = ns;
      CalculateSumSquares = calculateSumSquares;
      _percentiles = new List<PercentileConfig>();
    }

    internal bool AddPercentile(PercentileConfig percentile)
    {
      // Check for duplicates
      if (_percentiles.Any(p => p.Threshold == percentile.Threshold && p.FlushInterval == percentile.FlushInterval))
      {
        return false;
      }
      _percentiles.Add(percentile);
      return true;
    }
  }

  public class PercentileConfig
  {
    public string Name { get; set; }
    public int Threshold { get; set; }
    public TimeSpan FlushInterval { get; set; }

    public PercentileConfig(string name, int threshold, TimeSpan flushInterval)
    {
      Name = name;
      Threshold = threshold;
      FlushInterval = flushInterval;
    }
  }
}
