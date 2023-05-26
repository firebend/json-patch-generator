using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;

namespace Firebend.JsonPatch;

public interface IJsonPatchGenerator
{
    JsonPatchDocument<T> Generate<T>(T a, T b, JsonSerializerSettings settings = null)
        where T : class;

    JsonPatchDocument ConvertFromGeneric<T>(JsonPatchDocument<T> patch) where T : class;
}
