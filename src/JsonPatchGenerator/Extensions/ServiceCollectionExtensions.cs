using System;
using Firebend.JsonPatch.Interfaces;
using Firebend.JsonPatch.JsonSerializationSettings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;

namespace Firebend.JsonPatch.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IJsonPatchGenerator"/> and all required dependencies to the service collection
    /// </summary>
    /// <param name="collection">
    /// The service collection
    /// </param>
    /// <param name="configureSettings">
    /// A callback to configure the default json serialization settings.
    /// </param>
    /// <param name="removePreviousRegistrations">
    /// True if all other registrations of any of the dependencies should be removed; otherwise, false.
    /// </param>
    /// <returns>
    /// The service collection
    /// </returns>
    public static IServiceCollection AddJsonPatchGenerator(this IServiceCollection collection,
        Func<JsonSerializerSettings, JsonSerializerSettings> configureSettings = null,
        bool removePreviousRegistrations = false)
    {
        var settings = DefaultJsonSerializationSettings.Configure();

        if (configureSettings is not null)
        {
            settings = configureSettings(settings);
            DefaultJsonSerializationSettings.Settings = settings;
        }

        if (removePreviousRegistrations)
        {
            collection.RemoveAll<IJsonDiffSettingsProvider>();
            collection.RemoveAll<IJsonPatchWriter>();
            collection.RemoveAll<IJsonPatchGenerator>();
            collection.RemoveAll<IJsonDiffDetector>();
        }

        collection.TryAddSingleton<IJsonDiffSettingsProvider>(new JsonDiffSettingsProvider(settings));
        collection.TryAddTransient<IJsonPatchWriter, JsonPatchWriter>();
        collection.TryAddTransient<IJsonPatchGenerator, DefaultJsonPatchGenerator>();
        collection.TryAddTransient<IJsonDiffDetector, JsonDiffDetector>();

        return collection;
    }
}
