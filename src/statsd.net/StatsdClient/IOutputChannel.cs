using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsdClient
{
  internal interface IOutputChannel
  {
    void Send(string line);
  }
}
