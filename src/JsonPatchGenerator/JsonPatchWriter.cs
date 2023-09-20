using System;
using System.IO;
using System.Text;
using Firebend.JsonPatch.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Firebend.JsonPatch;

public class JsonPatchWriter : IJsonPatchWriter
{
    private bool _isOpen;
    private StringBuilder _stringBuilder;
    private StringWriter _stringWriter;
    private JsonWriter _writer;

    public void WriteAdd(string path, object value)
        => WriteOperation(path, "add", value);

    public void WriteReplace(string path, object value)
        => WriteOperation(path, "replace", value);

    public void WriteRemove(string path)
        => WriteOperation(path, "remove", null);

    public string Finish()
    {
        _writer?.WriteEndArray();
        _writer?.Close();
        _stringWriter?.Close();
        _isOpen = false;

        var result = _stringBuilder?.ToString();
        _stringBuilder?.Clear();
        _writer = null;
        _stringWriter = null;

        return result;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Open()
    {
        _stringBuilder = new StringBuilder();
        _stringWriter = new StringWriter(_stringBuilder);
        _writer = new JsonTextWriter(_stringWriter);
        _writer.CloseOutput = false;
        _writer.AutoCompleteOnClose = false;
        _writer.WriteStartArray();
        _isOpen = true;
    }

    protected virtual void WriteOperation(string path, string operation, object value)
    {
        if (_isOpen is false)
        {
            Open();
        }

        if (_writer is null)
        {
            return;
        }

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

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _writer?.Close();
            _stringWriter?.Dispose();
            _stringBuilder?.Clear();
        }
    }

    ~JsonPatchWriter()
    {
        Dispose(false);
    }
}
