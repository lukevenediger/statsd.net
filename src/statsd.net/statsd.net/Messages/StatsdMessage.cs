using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.Messages
{
  public abstract class StatsdMessage
  {
    public string Name { get; set; }
    public MessageType MessageType { get; protected set; }
    
    public StatsdMessage()
    {
    }
  }
}
