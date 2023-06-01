using System;
using System.Linq;
using Firebend.JsonPatch.Interfaces;
using Firebend.JsonPatch.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;

namespace Firebend.JsonPatch;

public class DefaultJsonPatchGenerator : IJsonPatchGenerator
{
    private readonly IJsonDiffDetector _diffDetector;
    private readonly IJsonPatchWriter _writer;
    private readonly IJsonDiffSettingsProvider _settings;
    public DefaultJsonPatchGenerator(IJsonDiffDetector diffDetector, IJsonPatchWriter writer, IJsonDiffSettingsProvider settings)
    {
        _diffDetector = diffDetector;
        _writer = writer;
        _settings = settings;
    }

    public JsonPatchDocument<T> Generate<T>(T original, T modified)
        where T : class
    {
        if (original is null)
        {
            throw new ArgumentNullException(nameof(original));
        }

        if (modified is null)
        {
            throw new ArgumentNullException(nameof(modified));
        }

        if (ReferenceEquals(original, modified))
        {
            return new();
        }

        var diffs = _diffDetector.DetectChanges(original, modified);

        foreach (var jsonDiff in diffs)
        {
            switch (jsonDiff.Change)
            {
                case JsonChange.Unknown:
                    break;
                case JsonChange.Add:
                    _writer.WriteAdd(jsonDiff.Path, jsonDiff.Value);
                    break;
                case JsonChange.Replace:
                    _writer.WriteReplace(jsonDiff.Path, jsonDiff.Value);
                    break;
                case JsonChange.Remove:
                    _writer.WriteRemove(jsonDiff.Path);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var patchJson = _writer.Finish();

        var s = _settings.Get();

        var doc = JsonConvert.DeserializeObject<JsonPatchDocument<T>>(patchJson, s);
        return doc;
    }

    public JsonPatchDocument ConvertFromGeneric<T>(JsonPatchDocument<T> patch) where T : class
    {
        var genericOperations = patch
            .Operations.
            Select(x => new Operation { op = x.op, path = x.path, from = x.from, value = x.value })
            .ToList();

        var newPatch = new JsonPatchDocument(genericOperations, patch.ContractResolver);

        return newPatch;
    }
}
