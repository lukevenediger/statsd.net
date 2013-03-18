using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsdClient
{
  internal sealed class NullOutputChannel : IOutputChannel
  {
    public void Send(string line)
    {
      // noop
    }
  }
}
