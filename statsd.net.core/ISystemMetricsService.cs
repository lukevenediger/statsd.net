namespace statsd.net.core
{
  public interface ISystemMetricsService
  {
    void LogCount(string name, int quantity = 1);
    void LogGauge(string name, int value);
    bool HideSystemStats { get; set; }
  }
}
