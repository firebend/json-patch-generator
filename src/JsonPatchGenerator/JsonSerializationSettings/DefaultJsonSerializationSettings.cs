using Newtonsoft.Json;

namespace Firebend.JsonPatch.JsonSerializationSettings;

public static class DefaultJsonSerializationSettings
{
    public static JsonSerializerSettings Settings = Configure();

    public static JsonSerializerSettings Configure(JsonSerializerSettings serializerSettings = null)
    {
        serializerSettings ??= new JsonSerializerSettings();

        serializerSettings.NullValueHandling = NullValueHandling.Ignore;
        serializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
        serializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

        return serializerSettings;
    }
}
