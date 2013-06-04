using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Messages
{
  public class InvalidMessage : StatsdMessage
  {
    private static InvalidMessage _instance = new InvalidMessage();
    public static InvalidMessage Instance { get { return _instance; } }

    private InvalidMessage()
    {
      MessageType = MessageType.Invalid;
    }
  }
}
