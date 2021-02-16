using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Firebend.JsonPatch.JsonSerializationSettings;
using Newtonsoft.Json;

namespace Firebend.JsonPatch.Extensions
{
    public static class Extensions
    {
        public static bool IsObject(this Type type)
        {
            var info = type?.GetTypeInfo();

            return !(type is null) && !(type == ObjectTypes.String || type.IsValueType || info.IsPrimitive) && Nullable.GetUnderlyingType(type) == null;
        }

        public static bool IsCollection(this Type type)
        {
            return !(type is null) && !ReferenceEquals(type, ObjectTypes.String) && ObjectTypes.Enumerable.GetTypeInfo().IsAssignableFrom(type);
        }

        public static Type CollectionInnerType(this Type type)
        {
            return (type?.GenericTypeArguments?.FirstOrDefault()) ?? type.GetElementType();
        }

        public static IEnumerable<Type> GetChildPropertyTypes(this Type type)
        {
            return new[] { type }.GetChildPropertyTypes();
        }

        public static Type[] GetChildPropertyTypes(this IEnumerable<Type> newTypes, List<Type> allTypes = null)
        {
            if (allTypes == null) allTypes = new List<Type>();

            var typesToAdd = new List<Type>();

            foreach (var type in newTypes)
            {
                if (type == null || type.IsPrimitive || type == ObjectTypes.String)
                {
                    continue;
                }

                allTypes.Add(type);

                var nullableType = Nullable.GetUnderlyingType(type);

                if (nullableType != null)
                {
                    if (!allTypes.Contains(nullableType) 
                        && !typesToAdd.Contains(nullableType))
                    {
                        typesToAdd.Add(nullableType);
                    }
                }
                else if (type.IsArray)
                {
                    var innerType = type.GetElementType();
                    if (!allTypes.Contains(innerType) 
                        && !typesToAdd.Contains(innerType))
                    {
                        typesToAdd.Add(innerType);
                    }
                }
                else if (type.IsGenericType)
                {
                    var types = type.GetGenericArguments()
                                    .Except(allTypes)
                                    .Except(typesToAdd);

                    typesToAdd.AddRange(types);
                }
                else if (type.IsObject())
                {
                    var props = type.GetProperties(BindingFlags.Public|BindingFlags.Instance);

                    var types = props.Select(x => x.PropertyType)
                                     .Except(allTypes)
                                     .Except(typesToAdd);

                    typesToAdd.AddRange(types);   
                }
            }

            if (!typesToAdd.IsEmpty())
            {
                allTypes.AddRange(typesToAdd.GetChildPropertyTypes(allTypes).AsArray());
            }

            return allTypes.Distinct().AsArray();
        }

        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return (source == null) || !source.Any();
        }

        public static T[] AsArray<T>(this IEnumerable<T> source)
        {
            return source == null ? new T[0] : source as T[] ?? source.ToArray();
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue))
        {
            return dict.ContainsKey(key) ? dict[key] : defaultValue;
        }

        public static T Clone<T>(this T source) where T : class
        {
            return ((object) source).Clone<T>();
        }

        public static TOut Clone<TOut>(this object source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(source, null)) return default;

            var settings = DefaultJsonSerializationSettings.Configure();

            return JsonConvert.DeserializeObject<TOut>(JsonConvert.SerializeObject(source, settings), settings);
        }

        public static bool EqualsIgnoreCaseAndWhitespace(this string source, string compare)
        {
            if (source == null && compare == null)
            {
                return true;
            }

            if (source == null || compare == null)
            {
                return false;
            }

            return source.SafeTrim().Equals(compare.SafeTrim(), StringComparison.OrdinalIgnoreCase);
        }

        public static string SafeTrim(this string source)
        {
            return string.IsNullOrEmpty(source) ? source : source.Trim();
        }
    }

    public static class ObjectTypes
    {
        public static readonly Type DateTime = typeof(DateTime);
        public static readonly Type DateTimeNullable = typeof(DateTime?);
        public static readonly Type DateTimeOffset = typeof(DateTimeOffset);
        public static readonly Type DateTimeOffsetNullable = typeof(DateTimeOffset?);
        public static readonly Type Long = typeof(long);
        public static readonly Type Guid = typeof(Guid);
        public static readonly Type Int = typeof(int);
        public static readonly Type Byte = typeof(byte);
        public static readonly Type ByteNullable = typeof(byte?);
        public static readonly Type String = typeof(string);      
        public static readonly Type Decimal = typeof(decimal);
        public static readonly Type Enumerable = typeof(IEnumerable);
    }   
}