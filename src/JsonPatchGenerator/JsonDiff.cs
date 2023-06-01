namespace Firebend.JsonPatch;

public class JsonDiff
{
    public string Path { get; set; }

    public object Value { get; set; }

    public JsonChange Change { get; set; }

    public JsonDiff(string path, object value, JsonChange change)
    {
        Path = path;
        Value = value;
        Change = change;
    }

    public static JsonDiff Add(string path, object value) => new(path, value, JsonChange.Add);

    public static JsonDiff Remove(string path) => new(path, null, JsonChange.Remove);

    public static JsonDiff Replace(string path, object value) => new(path, value, JsonChange.Replace);
}
