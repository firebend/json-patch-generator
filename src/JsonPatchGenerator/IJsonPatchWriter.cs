namespace Firebend.JsonPatch;

public interface IJsonPatchWriter
{
    void WriteAdd(string path, object value);

    void WriteReplace(string path, object value);

    void WriteRemove(string path);

    string Finish();
}
