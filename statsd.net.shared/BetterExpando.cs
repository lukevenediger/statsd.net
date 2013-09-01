using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared
{
  public class BetterExpando : DynamicObject
  {
    private Dictionary<string, object> _dict;
    private bool _ignoreCase;
    private bool _returnEmptyStringForMissingProperties;

    /// <summary>
    /// Creates a BetterExpando object/
    /// </summary>
    /// <param name="ignoreCase">Don't be strict about property name casing.</param>
    /// <param name="returnEmptyStringForMissingProperties">If true, returns String.Empty for missing properties.</param>
    /// <param name="root">An ExpandoObject to consume and expose.</param>
    public BetterExpando(bool ignoreCase = false, 
      bool returnEmptyStringForMissingProperties = false,
      ExpandoObject root = null)
    {
      _dict = new Dictionary<string, object>();
      _ignoreCase = ignoreCase;
      _returnEmptyStringForMissingProperties = returnEmptyStringForMissingProperties;
      if (root != null)
      {
        Augment(root);
      }
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
      UpdateDictionary(binder.Name, value);
      return true;
    }

    public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
    {
      if (indexes[0] is string)
      {
        var key = indexes[0] as string;
        UpdateDictionary(NormalisePropertyName(key), value);
        return true;
      }
      else
      {
        return base.TrySetIndex(binder, indexes, value);
      }
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
      var key = NormalisePropertyName(binder.Name);
      if (_dict.ContainsKey(key))
      {
        result = _dict[key];
        return true;
      }
      if ( _returnEmptyStringForMissingProperties )
      {
        result = String.Empty;
        return true;
      }
      return base.TryGetMember(binder, out result);
    }

    /// <summary>
    /// Combine two instances together to get a union.
    /// </summary>
    /// <returns>This instance but with additional properties</returns>
    /// <remarks>Existing properties are not overwritten.</remarks>
    public dynamic Augment(BetterExpando obj)
    {
      obj._dict
        .Where(pair => !_dict.ContainsKey(NormalisePropertyName(pair.Key)))
        .ToList()
        .ForEach(pair => UpdateDictionary(pair.Key, pair.Value));
      return this;
    }

    public dynamic Augment(ExpandoObject obj)
    {
      ((IDictionary<string, object>)obj)
        .Where(pair => !_dict.ContainsKey(NormalisePropertyName(pair.Key)))
        .ToList()
        .ForEach(pair => UpdateDictionary(pair.Key, pair.Value));
      return this;
    }

    public T ValueOrDefault<T>(string propertyName, T defaultValue)
    {
      propertyName = NormalisePropertyName(propertyName);
      return _dict.ContainsKey(propertyName)
        ? (T)_dict[propertyName]
        : defaultValue;
    }

    /// <summary>
    /// Check if BetterExpando contains a property.
    /// </summary>
    /// <remarks>Respects the case sensitivity setting</remarks>
    public bool HasProperty(string name)
    {
      return _dict.ContainsKey(NormalisePropertyName(name));
    }

    /// <summary>
    /// Returns this object as comma-separated name-value pairs.
    /// </summary>
    public override string ToString()
    {
      return String.Join(", ", _dict.Select(pair => pair.Key + " = " + pair.Value ?? "(null)").ToArray());
    }

    private void UpdateDictionary(string name, object value)
    {
      var key = NormalisePropertyName(name);
      if (_dict.ContainsKey(key))
      {
        _dict[key] = value;
      }
      else
      {
        _dict.Add(key, value);
      }
    }

    private string NormalisePropertyName(string propertyName)
    {
      return _ignoreCase ? propertyName.ToLower() : propertyName;
    } 
  }
}