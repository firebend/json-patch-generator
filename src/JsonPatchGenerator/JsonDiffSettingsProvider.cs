using Newtonsoft.Json;

namespace Firebend.JsonPatch;

public class JsonDiffSettingsProvider : IJsonDiffSettingsProvider
{
    private readonly JsonSerializerSettings _settings;

    public JsonDiffSettingsProvider(JsonSerializerSettings settings)
    {
        _settings = settings;
    }

    public JsonSerializerSettings Get() => _settings;
}
