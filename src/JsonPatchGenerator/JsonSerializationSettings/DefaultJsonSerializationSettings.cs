using Newtonsoft.Json;

namespace Firebend.JsonPatch.JsonSerializationSettings;

public static class DefaultJsonSerializationSettings
{
#pragma warning disable IDE1006
    // ReSharper disable once InconsistentNaming
    public static JsonSerializerSettings Settings = Configure();
#pragma warning restore IDE1006

    /// <summary>
    /// Configures default settings for this json patch generating library
    /// </summary>
    /// <param name="serializerSettings">
    /// Any settings that could have been preconfigured.
    /// </param>
    /// <returns>
    /// The settings with defaults applied.
    /// </returns>
    public static JsonSerializerSettings Configure(JsonSerializerSettings serializerSettings = null)
    {
        serializerSettings ??= new JsonSerializerSettings();

        serializerSettings.NullValueHandling = NullValueHandling.Ignore;
        serializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
        serializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
        serializerSettings.Formatting = Formatting.None;
        serializerSettings.TypeNameHandling = TypeNameHandling.Objects;

        return serializerSettings;
    }
}
