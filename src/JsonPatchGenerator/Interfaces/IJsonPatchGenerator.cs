using Microsoft.AspNetCore.JsonPatch;

namespace Firebend.JsonPatch.Interfaces;

public interface IJsonPatchGenerator
{
    JsonPatchDocument<T> Generate<T>(T original, T modified)
        where T : class;

    JsonPatchDocument ConvertFromGeneric<T>(JsonPatchDocument<T> patch) where T : class;
}
