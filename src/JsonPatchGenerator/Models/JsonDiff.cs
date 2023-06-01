namespace Firebend.JsonPatch.Models;

public class JsonDiff
{
    /// <summary>
    /// The field path for the json patch diff
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// The value the path should be changed to
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Which kind of json patch operation happened
    /// </summary>
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
