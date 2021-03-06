using Microsoft.AspNetCore.JsonPatch;

namespace Firebend.JsonPatch
{
    public interface IJsonPatchGenerator
    {
        JsonPatchDocument<T> Generate<T>(T a, T b)
            where T : class;

        JsonPatchDocument ConvertFromGeneric<T>(JsonPatchDocument<T> patch) where T : class;
    }
}
