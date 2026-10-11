using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace I18Next.Net.Internal;

/// <summary>
///     Converts translation arguments, e.g. anonymous objects like <c>new { count = 2 }</c>, to dictionaries.
/// </summary>
public static class ObjectExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyAccessor[]> AccessorCache = new();

    /// <summary>
    ///     Converts an object to a dictionary of its public instance properties. The property getters are cached per type and
    ///     compiled to delegates where dynamic code is supported. Use dictionaries or the source generator of the
    ///     I18Next.Net.Generators package in trimmed applications, because the properties are read with reflection.
    /// </summary>
    /// <param name="value">The object to convert.</param>
    /// <returns>
    ///     The values keyed by their property names, the value itself if it already is a
    ///     <see cref="IDictionary{TKey,TValue}" /> of string and object, or an empty dictionary if it is null.
    /// </returns>
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

    /// <inheritdoc cref="ObjectToDictionary" />
    public static IDictionary<string, object> ToDictionary(this object value)
    {
        return ObjectToDictionary(value);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Trimmed applications pass dictionaries or get anonymous arguments converted to dictionaries by the I18Next.Net.Generators package.")]
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
