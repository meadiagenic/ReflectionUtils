using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Dynamic;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Collections;

namespace ReflectionUtils
{
    public static class ReflectionExtensions
    {
        private static IDictionary<Type, PropertyInfo[]> _propertyCache = new PopulatingDictionary<Type, PropertyInfo[]>(t => t.GetProperties());
        public static IEnumerable<PropertyInfo> GetProperties(this object @obj)
        {
            if (@obj == null) yield break;

            foreach (var item in _propertyCache[@obj.GetType()]) {
                yield return item;    
            }
            
        }

        public static IDictionary<string, object> ToDictionary(this object @obj)
        {
            return (IDictionary<string, object>)@obj.ToDynamic();
        }

        public static dynamic ToDynamic(this object @obj)
        {
            var result = new ExpandoObject();

            var d = result as IDictionary<string, object>;

            if (@obj.GetType() == typeof(ExpandoObject)) return @obj;
            if (@obj.GetType() == typeof(NameValueCollection))
            {
                var nv = (NameValueCollection)@obj;
                nv.Cast<string>()
                    .Select(key => new KeyValuePair<string, object>(key, nv[key]))
                    .ToList()
                    .ForEach(i => d.Add(i));
            }
            else
            {
                var props = @obj.GetProperties();
                foreach (var item in props)
                {
                    d.Add(item.Name, item.GetValue(@obj, null));
                }
            }
            return result;
        }
    }

    public class PopulatingDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private ConcurrentDictionary<TKey, TValue> _dictionary = new ConcurrentDictionary<TKey, TValue>();

        
        public PopulatingDictionary(Func<TKey, TValue> onMissing)
        {
            if (onMissing == null) throw new ArgumentException();
            OnMissing = onMissing;
        }

        public Func<TKey, TValue> OnMissing { get; private set; }

        public void Add(TKey key, TValue value)
        {
            _dictionary.TryAdd(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return _dictionary.Keys; }
        }

        public bool Remove(TKey key)
        {
            TValue val = default(TValue);
            return _dictionary.TryRemove(key, out val);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return _dictionary.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                return _dictionary.GetOrAdd(key, OnMissing);
            }
            set
            {
                _dictionary.AddOrUpdate(key, value, (k, ov) => value);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionary.TryAdd(item.Key, item.Value);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
