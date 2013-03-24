using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsdClient
{
  public class StatsBuilder
  {
    public static dynamic count
    {
      get
      {
        return new StatsBuilderInternal(MetricType.COUNT, 1);
      }
    }

    public static dynamic timing
    {
      get
      {
        return new StatsBuilderInternal(MetricType.TIMING);
      } 
    }

    public static dynamic gauge
    {
      get
      {
        return new StatsBuilderInternal(MetricType.GAUGE);
      }
    }

    private class StatsBuilderInternal : DynamicObject
    {
      private List<string> _parts;
      private int? _quantity;
      private string _metricType;

      public StatsBuilderInternal(string metricType, int? defaultQuantity = null)
      {
        _parts = new List<string>();
        _quantity = defaultQuantity;
        _metricType = metricType;
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
        return String.Join(".", _parts) + ":" + _quantity + "|" + _metricType;
      }
    }
  }
}
