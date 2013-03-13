using statsd.net.Messages;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net_Tests.Infrastructure
{
  public class _
  {
    public static dynamic count
    {
      get
      {
        return new StatsBuilderInternal(MessageType.Counter, 1);
      }
    }

    public static dynamic timing
    {
      get
      {
        return new StatsBuilderInternal(MessageType.Timing);
      } 
    }

    public static dynamic gauge
    {
      get
      {
        return new StatsBuilderInternal(MessageType.Gauge);
      }
    }

    private class StatsBuilderInternal : DynamicObject
    {
      private List<string> _parts;
      private int? _quantity;
      private string _messageTypeCode;

      public StatsBuilderInternal(MessageType messageType, int? defaultQuantity = null)
      {
        _parts = new List<string>();
        switch (messageType)
        {
          case MessageType.Counter: _messageTypeCode = "c"; break;
          case MessageType.Gauge: _messageTypeCode = "g"; break;
          case MessageType.Set: _messageTypeCode = "s"; break;
          case MessageType.Timing: _messageTypeCode = "ms"; break;
        }
        _quantity = defaultQuantity;
      }

      public override bool TryGetMember(GetMemberBinder binder, out object result)
      {
        _parts.Add(binder.Name);
        result = this;
        return true;
      }

      public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
      {
        if (binder.Name == "_" && args.Length == 1)
        {
          _parts.Add(args[0].ToString());
          result = this;
          return true;
        }
        result = null;
        return false;
      }

      public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
      {
        if (binder.Operation == System.Linq.Expressions.ExpressionType.Add)
        {
          _quantity = (int)arg;
          result = this.ToString();
          return true;
        }
        result = null;
        return false;
      }

      public override string ToString()
      {
        if (_quantity == null)
        {
          throw new InvalidOperationException("Must specify a quantity.");
        }
        return String.Join(".", _parts) + ":" + _quantity + "|" + _messageTypeCode;
      }
    }
  }
}
