using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.System
{
  public class SuperCheapIOC
  {
    private static SuperCheapIOC _instance;

    private Dictionary<string, object> _items;
    
    static SuperCheapIOC()
    {
      _instance = new SuperCheapIOC();
    }

    private SuperCheapIOC()
    {
      _items = new Dictionary<string, object>();
    }

    public static SuperCheapIOC Instance { get { return _instance; } }

    public static T Resolve<T>()
    {
      return (T)_instance._items[typeof(T).Name];
    }

    public static void Add<T>(T instance)
    {
      _instance._items.Add(typeof(T).Name, instance);
    }
  }
}
