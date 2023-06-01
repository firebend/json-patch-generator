using Newtonsoft.Json;

namespace Firebend.JsonPatch;

public interface IJsonDiffSettingsProvider
{
    JsonSerializerSettings Get();
}
