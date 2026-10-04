using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace I18Next.Net.Internal;

public static class ObjectExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyAccessor[]> AccessorCache = new();

    public static IDictionary<string, object> ObjectToDictionary(object value)
    {
        if (value == null)
            return new Dictionary<string, object>();

        if (value is IDictionary<string, object> dictionary)
            return dictionary;

        var accessors = AccessorCache.GetOrAdd(value.GetType(), CreateAccessors);
        var result = new Dictionary<string, object>(accessors.Length);

        for (var i = 0; i < accessors.Length; i++)
            result.Add(accessors[i].Name, accessors[i].Getter(value));

        return result;
    }

    internal static string ToInvariantString(this object value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal
            ? ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture)
            : value?.ToString();
    }

    public static IDictionary<string, object> ToDictionary(this object value)
    {
        return ObjectToDictionary(value);
    }

    private static PropertyAccessor[] CreateAccessors(Type type)
    {
        return
        [
            .. type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(prop => prop.GetIndexParameters().Length == 0 && prop.GetMethod != null)
                .Select(prop => new PropertyAccessor(prop.Name, CreateGetter(type, prop)))
        ];
    }

    private static Func<object, object> CreateGetter(Type type, PropertyInfo property)
    {
#if NET
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled)
            return property.GetValue;
#endif

        var instance = Expression.Parameter(typeof(object), "instance");
        var body = Expression.Convert(Expression.Property(Expression.Convert(instance, type), property), typeof(object));

        return Expression.Lambda<Func<object, object>>(body, instance).Compile();
    }

    private readonly struct PropertyAccessor(string name, Func<object, object> getter)
    {
        public Func<object, object> Getter { get; } = getter;

        public string Name { get; } = name;
    }
}
