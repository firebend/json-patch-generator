using Newtonsoft.Json;

namespace Firebend.JsonPatchGenerator.JsonSerializationSettings
{
    public static class DefaultJsonSerializationSettings
    {
        public static JsonSerializerSettings Configure(JsonSerializerSettings serializerSettings = null)
        {
            if (serializerSettings == null)
            {
                serializerSettings = new JsonSerializerSettings();
            }

            serializerSettings.NullValueHandling = NullValueHandling.Ignore;
            serializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
            serializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

            return serializerSettings;
        }
    }
}