using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Firebend.JsonPatch.JsonSerializationSettings;
using Newtonsoft.Json;

namespace Firebend.JsonPatch.Extensions;

public static class Extensions
{
    public static bool IsObject(this Type type)
    {
        var info = type?.GetTypeInfo();

        return type is not null && !(type == ObjectTypes.String || type.IsValueType || info.IsPrimitive) && Nullable.GetUnderlyingType(type) == null;
    }

    public static bool IsCollection(this Type type) =>
        type is not null && !ReferenceEquals(type, ObjectTypes.String) && ObjectTypes.Enumerable.GetTypeInfo().IsAssignableFrom(type);

    public static Type CollectionInnerType(this Type type) =>
        type?.GenericTypeArguments?.FirstOrDefault() ?? type.GetElementType();

    public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default) =>
        dict.ContainsKey(key) ? dict[key] : defaultValue;

    public static T Clone<T>(this T source) where T : class => ((object)source).Clone<T>();

    public static TOut Clone<TOut>(this object source)
    {
        // Don't serialize a null object, simply return the default for that object
        if (ReferenceEquals(source, null))
        {
            return default;
        }

        var settings = DefaultJsonSerializationSettings.Configure();

        return JsonConvert.DeserializeObject<TOut>(JsonConvert.SerializeObject(source, settings), settings);
    }

    public static bool EqualsIgnoreCaseAndWhitespace(this string source, string compare)
    {
        if (source is null && compare is null)
        {
            return true;
        }

        if (source is null || compare is null)
        {
            return false;
        }

        return source.SafeTrim().Equals(compare.SafeTrim(), StringComparison.OrdinalIgnoreCase);
    }

    public static string SafeTrim(this string source) => string.IsNullOrEmpty(source) ? source : source.Trim();
}

public static class ObjectTypes
{
    public static readonly Type String = typeof(string);
    public static readonly Type Enumerable = typeof(IEnumerable);
}
