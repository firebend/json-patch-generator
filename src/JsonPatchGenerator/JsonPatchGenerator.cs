using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Firebend.JsonPatch.Extensions;
using Firebend.JsonPatch.JsonSerializationSettings;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Firebend.JsonPatch
{
    public class JsonPatchGenerator : IJsonPatchGenerator
    {
        /// <summary>
        ///     Generates a JsonPatchDocument by comparing two objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a">The original object</param>
        /// <param name="b">The modified object</param>
        /// <returns>The <see cref="JsonPatchDocument" /></returns>
        public JsonPatchDocument<T> Generate<T>(T a, T b)
            where T : class
        {
            var output = new JsonPatchDocument<T>();

            if (ReferenceEquals(a, b))
            {
                return output;
            }

            var settings = DefaultJsonSerializationSettings.Configure();
            var jsonSerializer = JsonSerializer.Create(settings);

            var originalJson = JObject.FromObject(a, jsonSerializer);
            var modifiedJson = JObject.FromObject(b, jsonSerializer);

            var propertyInfos = GetPropertyInfos(b.GetType());
            FillJsonPatchValues(originalJson, modifiedJson, output, propertyInfos);

            return output;
        }

        public static IDictionary<string, PropertyInfo> GetPropertyInfos(Type type, string currentPath = "/", Dictionary<(string, string), (string, PropertyInfo)> dictionaryProps = null)
        {
            var gotProps = false;

            if(dictionaryProps == null)
            {
                dictionaryProps = new Dictionary<(string, string), (string, PropertyInfo)>();
            }

            var properties = type.GetProperties();
            foreach(var prop in properties)
            {
                var path = $"{currentPath}{prop.Name}";
                if(!dictionaryProps.ContainsKey((prop.PropertyType.FullName, prop.Name)))
                {
                    gotProps = true;
                    dictionaryProps[(prop.PropertyType.FullName, prop.Name)] = (path, prop);
                }
            }

            if(gotProps)
            {
                foreach(var prop in properties)
                {
                    var path = $"{currentPath}{prop.Name}/";
                    if(prop.PropertyType.IsCollection())
                    {            
                        GetPropertyInfos(prop.PropertyType.CollectionInnerType(), path, dictionaryProps);
                    }
                    else if(prop.PropertyType.IsObject())
                    {
                        GetPropertyInfos(prop.PropertyType, path, dictionaryProps);
                    }
                }
            }

            var dictProps = dictionaryProps.Select(x => x.Value).ToDictionary(x => x.Item1, x => x.Item2);
            return dictProps;
        }

        public static object GetValue(JToken value, string path,  IDictionary<string, PropertyInfo> propertyInfos, string operationType)
        {
            var propertyInfo = propertyInfos.Get(path);
                
            if(propertyInfo != null && (propertyInfo.PropertyType.IsObject() || propertyInfo.PropertyType.IsCollection()))
            {
                if(operationType == "add")
                {
                    var json = value.ToString();
                    if(propertyInfo.PropertyType.IsCollection())
                    {
                        try
                        {
                            var element = JsonConvert.DeserializeObject(json, propertyInfo.PropertyType.CollectionInnerType());
                            return element;
                        }
                        catch(Exception)
                        {
                            return JsonConvert.DeserializeObject(json, propertyInfo.PropertyType);        
                        }
                    }
                    return JsonConvert.DeserializeObject(json, propertyInfo.PropertyType);
                }
                else if(operationType == "replace")
                {
                    var json = value.ToString();
                    if(propertyInfo.PropertyType.IsCollection())
                    {
                        try
                        {
                            var innerCollectionType = propertyInfo.PropertyType.CollectionInnerType();
                            if(innerCollectionType.IsObject() || innerCollectionType.IsCollection())
                            {
                                var element = JsonConvert.DeserializeObject(json, propertyInfo.PropertyType.CollectionInnerType());
                                return element;
                            }
                            else
                            {
                                return json;
                            }
                        }
                        catch(Exception)
                        {
                            return JsonConvert.DeserializeObject(json, propertyInfo.PropertyType);        
                        }
                    }
                    return JsonConvert.DeserializeObject(json, propertyInfo.PropertyType);
                }
            }            
        
            return value.ToString();
        }

        /// <summary>
        ///     Fills the json patch values.
        /// </summary>
        /// <param name="originalJson">The original json.</param>
        /// <param name="modifiedJson">The modified json.</param>
        /// <param name="patch">The patch.</param>
        /// <param name="currentPath">The current path.</param>
        private static void FillJsonPatchValues<T>(JObject originalJson,
            JObject modifiedJson,
            JsonPatchDocument<T> patch,
            IDictionary<string, PropertyInfo> propertyInfos,
            string currentPath = "/",
            string currentSimplePath = "/")
            where T : class
        {
            var originalPropertyNames = new HashSet<string>(originalJson.Properties().Select(p => p.Name));
            var modifiedPropertyNames = new HashSet<string>(modifiedJson.Properties().Select(p => p.Name));

            // Remove properties not in modified.
            foreach (var propName in originalPropertyNames.Except(modifiedPropertyNames))
            {
                var path = $"{currentPath}{propName}";

                patch.Operations.Add(new Operation<T>("remove", path, null));
            }

            // Add properties not in original
            foreach (var propName in modifiedPropertyNames.Except(originalPropertyNames))
            {
                var prop = modifiedJson.Property(propName);
                var path = $"{currentPath}{propName}";
                var simplePath = $"{currentSimplePath}{propName}";
                
                patch.Operations.Add(new Operation<T>("add", path, null, GetValue(prop.Value, simplePath, propertyInfos, "add")));
            }

            // Modify properties that exist in both.
            foreach (var propName in originalPropertyNames.Intersect(modifiedPropertyNames))
            {
                var originalProp = originalJson.Property(propName);
                var modifiedProp = modifiedJson.Property(propName);

                if (originalProp.Value.Type != modifiedProp.Value.Type)
                {
                    var path = $"{currentPath}{propName}";
                    var simplePath = $"{currentSimplePath}{propName}";

                    patch.Operations.Add(new Operation<T>("replace", path, null, GetValue(modifiedProp.Value, simplePath, propertyInfos, "replace")));
                }
                else if (!string.Equals(originalProp.Value.ToString(Formatting.None), modifiedProp.Value.ToString(Formatting.None)))
                {
                    if (originalProp.Value.Type == JTokenType.Object)
                    {
                        // Recursively fill nested objects.
                        FillJsonPatchValues(originalProp.Value as JObject,
                            modifiedProp.Value as JObject,
                            patch, propertyInfos, $"{currentPath}{propName}/", $"{currentPath}{propName}/");
                    }
                    else if (modifiedProp.Value is JArray modifiedArray && originalProp.Value is JArray originalArray)
                    {
                        
                        var maxOriginalIndex = originalArray.Count - 1;
                        var path = $"{currentPath}{propName}";
                        var simplePath = $"{currentSimplePath}{propName}";

                        for (var modifiedIndex = 0; modifiedIndex < modifiedArray.Count; modifiedIndex++)
                        {
                            if (modifiedIndex > maxOriginalIndex)
                            {
                                //add an object to the patch array

                                patch.Operations.Add(new Operation<T>(
                                    "add",
                                    $"{path}/-",
                                    null,
                                    GetValue(modifiedArray[modifiedIndex], simplePath, propertyInfos, "add")));
                            }
                            else if (modifiedIndex <= maxOriginalIndex)
                            {
                                if (originalArray[modifiedIndex] is JObject originalObject
                                    && modifiedArray[modifiedIndex] is JObject modifiedObject)
                                {
                                    //replace an object from the patch array
                                    FillJsonPatchValues(originalObject,
                                        modifiedObject,
                                        patch,
                                        propertyInfos,
                                        $"{path}/{modifiedIndex}/",
                                        $"{path}/");
                                }
                                else if (originalArray[modifiedIndex]?.ToString() != modifiedArray[modifiedIndex]?.ToString())
                                {
                                    patch.Operations.Add(new Operation<T>(
                                        "replace",
                                        $"{path}/{modifiedIndex}",
                                        null,
                                        GetValue(modifiedArray[modifiedIndex], simplePath, propertyInfos, "replace")));
                                }
                            }
                        }

                        var diff = originalArray.Count - modifiedArray.Count;

                        if (diff > 0)
                        {
                            var counter = 0;

                            while (counter < diff)
                            {
                                patch.Operations.Add(new Operation<T>(
                                    "remove",
                                    $"{path}/{maxOriginalIndex - counter}",
                                    null));

                                counter++;
                            }
                        }
                    }
                    else
                    {
                        var path = $"{currentPath}{propName}";
                        var simplePath = $"{currentSimplePath}{propName}";
                        
                        // Simple Replace otherwise to make patches idempotent.
                        patch.Operations.Add(new Operation<T>(
                            "replace",
                            path,
                            null,
                            GetValue(modifiedProp.Value, simplePath, propertyInfos, "replace")));
                    }
                }
            }
        }

        public JsonPatchDocument ConvertToGeneric<T>(JsonPatchDocument<T> patch) where T : class
        {
            var genericOperations = new List<Operation>();
            foreach(var oper in patch.Operations)
            {
                var newOper = new Operation();
                newOper.op = oper.op;
                newOper.path = oper.path;
                newOper.from = oper.from;
                newOper.value = oper.value;

                genericOperations.Add(newOper);
            }
            var newPatch = new JsonPatchDocument(genericOperations, patch.ContractResolver);

            return newPatch;
        }
    }
}
