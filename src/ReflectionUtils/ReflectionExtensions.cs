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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection.Emit;

namespace ReflectionUtils
{
    public static class ReflectionExtensions
    {
        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && (typeof(Nullable<>) == type.GetGenericTypeDefinition());
        }

        public static bool IsConcrete(this Type type)
        {
            return (!type.IsInterface && !type.IsAbstract && !type.IsValueType);
        }

        public static void ThrowIfNull(this object @obj, string argumentName)
        {
            if (@obj == null) throw new ArgumentNullException(argumentName);
        }

        public static object CreateInstance(this Type type)
        {
            if (type == null || type.IsValueType || type.IsAbstract || type == typeof(object) || type == typeof(string))
            {
                return new ExpandoObject();
            }

            var targetType = type.ResolveInterfaceType();

            if (targetType.IsInterface)
            {
                return new ExpandoObject();
            }

            //TypeMap typemap = targetType.LoadTypeMap();
            //if ((typemap == null) || (typemap.Ctor == null) || ((typemap.CtorArgs != null) && (typemap.CtorArgs.Length > 0)))
            //{
            //    return new ExpandoObject();
            //}
            TypeMap typemap = null;
            return typemap.Ctor();
        }

        public static object CreateInstance(this Type type, object args)
        {
            var targetType = type.ResolveInterfaceType();

            return null;
        }

        

        private static GenericInterfaceSwitchCase giSwitchCase = new GenericInterfaceSwitchCase();

        public static Type ResolveGenericTypeDefinition(this Type type)
        {
            try
            {
                return type.GetGenericTypeDefinition();
            }
            catch (InvalidOperationException)
            {
            }
            return type;
        }

        public static Type ResolveInterfaceType(this Type type)
        {
            var targetType = type;
            if (type.IsInterface)
            {
                targetType = giSwitchCase.Eval(targetType);
            }
            return targetType;
        }
    }

    public class GenericInterfaceSwitchCase : Dictionary<Type, Func<Type, Type>>
    {

        public Type ConvertToListType(Type genericType)
        {
            Type[] genericArgs = genericType.GetGenericArguments();
            return typeof(List<>).MakeGenericType(genericArgs);
        }

        public GenericInterfaceSwitchCase()
        {
            Add(typeof(IList<>), ConvertToListType);
            Add(typeof(IEnumerable<>), ConvertToListType);
            Add(typeof(IQueryable<>), ConvertToListType);
            Add(typeof(IOrderedQueryable<>), ConvertToListType);
            Add(typeof(ICollection<>), ConvertToListType);
            Add(typeof(IDictionary<,>), t =>
            {
                Type[] genericArgs = t.GetGenericArguments();
                if (genericArgs.Length == 2 && genericArgs[0] == typeof(string) && genericArgs[1] == typeof(object))
                {
                    return typeof(ExpandoObject);
                }
                else
                {
                    return typeof(Dictionary<,>).MakeGenericType(genericArgs);
                }
            });
            Add(typeof(IList), t => typeof(object[]));
            Add(typeof(IEnumerable), t => typeof(object[]));
            Add(typeof(IQueryable), t => typeof(object[]));
            Add(typeof(IOrderedQueryable), t => typeof(object[]));
            Add(typeof(ICollection), t => typeof(object[]));
            Add(typeof(IDictionary), t => typeof(Dictionary<string, object>));
            Add(typeof(IDynamicMetaObjectProvider), t => typeof(ExpandoObject));
        }

        public Type Eval(Type value)
        {
            var genericType = value.ResolveGenericTypeDefinition();
            if (this.ContainsKey(genericType))
            {
                return this[genericType](value);
            }
            return value;
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


    public class TypeMap
    {
        public Factory Ctor { get; private set; }
    }
}
