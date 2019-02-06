using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace I18Next.Net.Internal
{
    public static class ObjectExtensions
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ReflectionCache = new ConcurrentDictionary<Type, PropertyInfo[]>();

        public static IDictionary<string, object> ObjectToDictionary(object value)
        {
            var dictionary = new Dictionary<string, object>();

            if (value == null)
                return dictionary;

            if (value is IDictionary<string, object>)
                return value as IDictionary<string, object>;

            foreach (var property in GetProperties(value))
                dictionary.Add(property.Name, property.GetValue(value));

            return dictionary;
        }

        public static IDictionary<string, object> ToDictionary(this object value)
        {
            return ObjectToDictionary(value);
        }

        private static IEnumerable<PropertyInfo> GetProperties(object instance)
        {
            var type = instance.GetType();

            if (ReflectionCache.TryGetValue(type, out var array))
                return array;

            var propertyInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(prop =>
                {
                    if (prop.GetIndexParameters().Length == 0)
                        return prop.GetMethod != (MethodInfo) null;

                    return false;
                });

            array = propertyInfos.ToArray();

            ReflectionCache.TryAdd(type, array);

            return array;
        }
    }
}
