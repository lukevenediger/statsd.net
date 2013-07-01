using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Messages
{
  public class InvalidMessage : StatsdMessage
  {
    public string Reason { get; private set; }

    public InvalidMessage() :
      this("Unknown")
    {
    }

    public InvalidMessage(string reason)
    {
      MessageType = MessageType.Invalid;
      Reason = reason;
    }
  }
}
