using System.Collections.Generic;
using System.Linq;
using Firebend.JsonPatch.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Firebend.JsonPatch;

public class JsonDiffDetector : IJsonDiffDetector
{
    private readonly IJsonDiffSettingsProvider _settings;

    private JsonSerializer _serializer;

    public JsonDiffDetector(IJsonDiffSettingsProvider settings)
    {
        _settings = settings;
    }

    private JsonSerializer Serializer => _serializer ??= JsonSerializer.Create(_settings.Get());

    public List<JsonDiff> DetectChanges(object original, object modified) => DetectChangesRecursion(GetJObject(original),
        GetJObject(modified),
        new List<JsonDiff>(),
        string.Empty);

    private JObject GetJObject(object a) => JObject.FromObject(a, Serializer);

    private static List<JsonDiff> DetectChangesRecursion(JObject original,
        JObject modified,
        List<JsonDiff> jsonDiffs,
        string currentPath)
    {
        var originalPropertyNames = new HashSet<string>(original.Properties().Select(p => p.Name));
        var modifiedPropertyNames = new HashSet<string>(modified.Properties().Select(p => p.Name));

        DetectRemovedProperties(currentPath, jsonDiffs, originalPropertyNames, modifiedPropertyNames);
        DetectAddedProperties(currentPath, jsonDiffs, originalPropertyNames, modifiedPropertyNames, modified);
        DetectModifiedProperties(currentPath, jsonDiffs, originalPropertyNames, modifiedPropertyNames, original, modified);

        return jsonDiffs;
    }

    private static void DetectAddedProperties(string currentPath,
        List<JsonDiff> diffs,
        IEnumerable<string> originalPropertyNames,
        IEnumerable<string> modifiedPropertyNames,
        JObject modified) => diffs
        .AddRange(modifiedPropertyNames
            .Except(originalPropertyNames)
            .Select(propName => JsonDiff.Add($"{currentPath}/{propName}", modified.Property(propName)?.Value)));

    private static void DetectRemovedProperties(string currentPath,
        List<JsonDiff> diffs,
        IEnumerable<string> originalPropertyNames,
        IEnumerable<string> modifiedPropertyNames) => diffs
        .AddRange(originalPropertyNames
            .Except(modifiedPropertyNames)
            .Select(propName => $"{currentPath}/{propName}")
            .Select(JsonDiff.Remove));

    private static void DetectModifiedProperties(string currentPath,
        List<JsonDiff> jsonDiffs,
        IEnumerable<string> originalPropertyNames,
        IEnumerable<string> modifiedPropertyNames,
        JObject original,
        JObject modified)
    {
        foreach (var propName in originalPropertyNames.Intersect(modifiedPropertyNames))
        {
            var originalProp = original.Property(propName);
            var modifiedProp = modified.Property(propName);

            if (originalProp is null || modifiedProp is null)
            {
                continue;
            }

            if (modifiedProp.Value.Type == JTokenType.Null)
            {
                jsonDiffs.Add(JsonDiff.Remove($"{currentPath}/{modifiedProp.Name}"));
                continue;
            }

            if (originalProp.Value.ToString(Formatting.None).EqualsIgnoreCaseAndWhitespace(modifiedProp.Value.ToString(Formatting.None)))
            {
                continue;
            }

            if (originalProp.Value.Type != modifiedProp.Value.Type)
            {
                jsonDiffs.Add(JsonDiff.Replace($"{currentPath}/{modifiedProp.Name}", modifiedProp.Value));
                continue;
            }

            switch (originalProp.Value.Type)
            {
                case JTokenType.Object:
                    DetectChangesRecursion(originalProp.Value as JObject,
                        modifiedProp.Value as JObject,
                        jsonDiffs,
                        $"{currentPath}/{propName}");

                    continue;
                case JTokenType.Array:
                    DetectArrayChanges(originalProp.Value as JArray,
                        modifiedProp.Value as JArray,
                        jsonDiffs,
                        $"{currentPath}/{propName}");

                    continue;
                default:
                    jsonDiffs.Add(JsonDiff.Replace($"{currentPath}/{propName}", modifiedProp.Value));
                    break;
            }
        }
    }

    private static void DetectArrayChanges(JArray original,
        JArray modified,
        List<JsonDiff> jsonDiffs,
        string currentPath)
    {
        if (DetectAddArrayElements(original, modified, jsonDiffs, currentPath))
        {
            return;
        }

        DetectRemoveArrayElements(original, modified, jsonDiffs, currentPath);
        DetectArrayElementUpdates(original, modified, jsonDiffs, currentPath);
    }

    private static void DetectArrayElementUpdates(JArray original, JArray modified, List<JsonDiff> jsonDiffs, string currentPath)
    {
        var maxOriginalIndex = original.Count - 1;

        for (var modifiedIndex = 0; modifiedIndex < modified.Count; modifiedIndex++)
        {
            if (modifiedIndex > maxOriginalIndex)
            {
                jsonDiffs.Add(JsonDiff.Add($"{currentPath}/-", modified[modifiedIndex]));
                continue;
            }

            if (original[modifiedIndex] is JObject originalObject
                && modified[modifiedIndex] is JObject modifiedObject)
            {
                DetectChangesRecursion(originalObject, modifiedObject, jsonDiffs, $"{currentPath}/{modifiedIndex}");
                continue;
            }

            if (original[modifiedIndex]?.ToString(Formatting.None) != modified[modifiedIndex]?.ToString(Formatting.None))
            {
                jsonDiffs.Add(JsonDiff.Replace($"{currentPath}/{modifiedIndex}", modified[modifiedIndex]));
            }
        }
    }

    private static void DetectRemoveArrayElements(JArray original, JArray modified, ICollection<JsonDiff> jsonDiffs, string currentPath)
    {
        var diff = original.Count - modified.Count;

        if (diff <= 0)
        {
            return;
        }

        var maxOriginalIndex = original.Count - 1;
        var counter = 0;

        while (counter < diff)
        {
            jsonDiffs.Add(JsonDiff.Remove($"{currentPath}/{maxOriginalIndex - counter}"));
            counter++;
        }
    }

    private static bool DetectAddArrayElements(JArray original, JArray modified, List<JsonDiff> jsonDiffs, string currentPath)
    {
        if (original.Count == 0 && modified.Count > 0)
        {
            jsonDiffs.AddRange(modified.Select((t, i) => JsonDiff.Add($"{currentPath}/{i}", t)));
            return true;
        }

        return false;
    }
}
