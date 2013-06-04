using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Services
{
  public interface ISystemInfoService
  {
    string HostName { get; }
  }

  public class SystemInfoService : ISystemInfoService
  {
    public string HostName { get; private set; }

    public SystemInfoService()
    {
      HostName = Environment.MachineName;
    }
  }
}
