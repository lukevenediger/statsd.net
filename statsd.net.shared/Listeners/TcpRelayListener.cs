using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using statsd.net.shared.Encryption;

namespace statsd.net.shared.Listeners
{
  /// <summary>
  /// Accepts relayed metrics over TCP
  /// </summary>
  public class TcpRelayListener : IListener
  {
    private SimplerAES _aes;

    public TcpRelayListener ( int port, string encodedKey, string encodedVector )
    {
      var key = Convert.FromBase64String(encodedKey);
      var vector = Convert.FromBase64String(encodedVector);
      _aes = new SimplerAES( key, vector );
    }

    public void LinkTo ( ITargetBlock<string> target, CancellationToken token )
    {
    }

    public bool IsListening
    {
      get { throw new NotImplementedException(); }
    }
  }
}
