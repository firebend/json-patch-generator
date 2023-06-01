namespace Firebend.JsonPatch.Interfaces;

public interface IJsonPatchWriter
{
    /// <summary>
    /// Writes a json patch add operation to the output.
    /// </summary>
    /// <param name="path">
    /// The json patch path
    /// </param>
    /// <param name="value">
    /// The value added.
    /// </param>
    void WriteAdd(string path, object value);

    /// <summary>
    /// Writes a json patch replace operation to the output.
    /// </summary>
    /// <param name="path">
    /// The json patch path
    /// </param>
    /// <param name="value">
    /// The value replaced.
    /// </param>
    void WriteReplace(string path, object value);

    /// <summary>
    /// Writes a json remove operation to the output
    /// </summary>
    /// <param name="path">
    /// The json patch path
    /// </param>
    void WriteRemove(string path);

    /// <summary>
    /// Finishes writing the json patch document and returns it as a string
    /// </summary>
    /// <returns>
    /// A json patch in string form.
    /// </returns>
    string Finish();
}
