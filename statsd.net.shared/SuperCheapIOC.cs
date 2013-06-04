using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared
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

    public static IEnumerable<T> ResolveAll<T>()
    {
      foreach(var key in _instance._items.Keys)
      {
        var reducedKey = key.Split('_')[0];
        if (reducedKey == typeof(T).Name)
        {
          yield return (T)_instance._items[key];
        }
      }
    }

    public static void Add<T>(T instance)
    {
      _instance._items.Add(typeof(T).Name, instance);
    }

    public static void Add<T>(T instance, string name)
    {
      _instance._items.Add(typeof(T).Name + "_" + name, instance);
    }

    public static void Reset()
    {
      _instance._items.Clear();
    }
  }
}
