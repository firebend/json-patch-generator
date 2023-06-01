using Microsoft.AspNetCore.JsonPatch;

namespace Firebend.JsonPatch.Interfaces;

public interface IJsonPatchGenerator
{

    /// <summary>
    /// Compares two objects and finds the differences in the two as a <see cref="JsonPatchDocument{TModel}"/>
    /// </summary>
    /// <param name="original">
    /// The model before changes were applied.
    /// </param>
    /// <param name="modified">
    /// The model after changes were applied.
    /// </param>
    /// <typeparam name="T">
    /// The model type
    /// </typeparam>
    /// <returns>
    /// A <see cref="JsonPatchDocument{TModel}"/> representing the changes in the two objects.
    /// </returns>
    JsonPatchDocument<T> Generate<T>(T original, T modified) where T : class;

    /// <summary>
    /// Converts a <see cref="JsonPatchDocument{TModel}"/> and converts it to the non-generic <see cref="JsonPatchDocument"/>
    /// </summary>
    /// <param name="patch">
    /// The generic patch document
    /// </param>
    /// <typeparam name="T">
    /// The type of model.
    /// </typeparam>
    /// <returns>
    /// A <see cref="JsonPatchDocument"/>
    /// </returns>
    JsonPatchDocument ConvertFromGeneric<T>(JsonPatchDocument<T> patch) where T : class;
}
