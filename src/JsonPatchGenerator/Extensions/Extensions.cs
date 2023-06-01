using System;
using Firebend.JsonPatch.JsonSerializationSettings;
using Newtonsoft.Json;

namespace Firebend.JsonPatch.Extensions;

public static class Extensions
{
    public static T Clone<T>(this T source, JsonSerializerSettings settings = null) where T : class
        => ((object)source).Clone<T>(settings);

    public static TOut Clone<TOut>(this object source, JsonSerializerSettings settings = null)
    {
        if (ReferenceEquals(source, null))
        {
            return default;
        }

        settings ??= DefaultJsonSerializationSettings.Settings;

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

    public static string SafeTrim(this string source)
        => string.IsNullOrEmpty(source) ? source : source.Trim();
}
