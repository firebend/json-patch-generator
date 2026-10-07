using Newtonsoft.Json;

namespace Firebend.JsonPatch.Interfaces;

public interface IJsonDiffSettingsProvider
{
    public JsonSerializerSettings Get();
}
