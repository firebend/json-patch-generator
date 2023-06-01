using System;
using Firebend.JsonPatch.Interfaces;
using Firebend.JsonPatch.JsonSerializationSettings;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Firebend.JsonPatch.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJsonPatchGenerator(this IServiceCollection collection, Func<JsonSerializerSettings, JsonSerializerSettings> configureSettings)
    {
        var settings = DefaultJsonSerializationSettings.Configure();

        if (configureSettings is not null)
        {
            settings = configureSettings(settings);
            DefaultJsonSerializationSettings.Settings = settings;
        }

        collection.AddSingleton<IJsonDiffSettingsProvider>(new JsonDiffSettingsProvider(settings));
        collection.AddSingleton<IJsonPatchWriter, JsonPatchWriter>();
        collection.AddSingleton<IJsonPatchGenerator, DefaultJsonPatchGenerator>();
        collection.AddSingleton<IJsonDiffDetector, JsonDiffDetector>();

        return collection;
    }
}
