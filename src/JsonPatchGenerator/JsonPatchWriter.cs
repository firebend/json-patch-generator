using System.IO;
using System.Text;
using Firebend.JsonPatch.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Firebend.JsonPatch;

public class JsonPatchWriter : IJsonPatchWriter
{
    private readonly StringBuilder _stringBuilder;
    private readonly JsonWriter _writer;

    public JsonPatchWriter()
    {
        _stringBuilder = new StringBuilder();
        _writer = new JsonTextWriter(new StringWriter(_stringBuilder));
        _writer.CloseOutput = true;
        _writer.AutoCompleteOnClose = true;
        _writer.WriteStartArray();
    }

    private void WriteOperation(string path, string operation, object value)
    {
        _writer.WriteStartObject();

        _writer.WritePropertyName("op");
        _writer.WriteValue(operation);

        _writer.WritePropertyName("path");
        _writer.WriteValue(path);

        if (operation != "remove")
        {
            _writer.WritePropertyName("value");

            switch (value)
            {
                case JObject jObject:
                    jObject.WriteTo(_writer);
                    break;
                case JArray jArray:
                    jArray.WriteTo(_writer);
                    break;
                default:
                    _writer.WriteValue(value);
                    break;
            }
        }

        _writer.WriteEndObject();
    }

    public void WriteAdd(string path, object value)
        => WriteOperation(path, "add", value);

    public void WriteReplace(string path, object value)
        => WriteOperation(path, "replace", value);

    public void WriteRemove(string path)
        => WriteOperation(path, "remove", null);

    public string Finish()
    {
        _writer.WriteEndArray();
        _writer.Close();
        return _stringBuilder.ToString();
    }
}
