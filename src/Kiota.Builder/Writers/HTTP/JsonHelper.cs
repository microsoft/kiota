using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Kiota.Builder.Writers.http
{
    internal class JsonHelper
    {
        /// <summary>
        /// Recursively processes a JSON schema to remove schema-specific fields (`type`, `allOf`, `oneOf`, `required`, `properties`),
        /// and replaces them with placeholders based on the type (e.g., "string" for strings, true for booleans, and first value for enums).
        /// If an `example` field is present, it returns the `example` value.
        /// In case of `oneOf`, only the first object is used.
        /// In case of `allOf`, objects are merged into a single one.
        /// </summary>
        /// <param name="obj">The JSON object (JObject) representing a schema.</param>
        /// <returns>A new JObject with schema fields stripped, or the `example` values if present.</returns>
        public static JObject StripJsonDownToRequestObject(JObject obj)
        {
            // Check if the schema contains a top-level "example" field, return it if found.
            if (obj["example"] is JObject example)
            {
                return example;
            }

            // Handle oneOf: take the first object
            if (obj.ContainsKey("oneOf") && obj["oneOf"] is JArray oneOfArray && oneOfArray.First is JObject firstSchema)
            {
                return StripJsonDownToRequestObject(firstSchema);
            }

            // Handle allOf: merge objects
            if (obj.ContainsKey("allOf") && obj["allOf"] is JArray allOfArray)
            {
                return MergeAllOfSchemas(allOfArray);
            }

            // Process properties if present
            if (obj.ContainsKey("properties"))
            {
                if (obj["properties"] is not JObject propertiesObject) return new JObject();

                return ProcessProperties(propertiesObject);
            }

            return obj;
        }

        /// <summary>
        /// Merges the objects in an allOf array.
        /// </summary>
        /// <param name="allOfArray">The array of objects (JArray) to merge.</param>
        /// <returns>A new JObject representing the merged object.</returns>
        private static JObject MergeAllOfSchemas(JArray allOfArray)
        {
            var mergedObject = new JObject();

            foreach (var schema in allOfArray)
            {
                if (schema is JObject schemaObject)
                {
                    var processedSchema = StripJsonDownToRequestObject(schemaObject);
                    mergedObject.Merge(processedSchema, new JsonMergeSettings
                    {
                        MergeArrayHandling = MergeArrayHandling.Union
                    });
                }
            }

            return mergedObject;
        }

        /// <summary>
        /// Processes the properties of a JSON schema object, replacing with values based on type.
        /// </summary>
        /// <param name="properties">The properties object (JObject) to process.</param>
        /// <returns>A new JObject with processed properties.</returns>
        private static JObject ProcessProperties(JObject properties)
        {
            var newProperties = new JObject();

            foreach (var property in properties)
            {
                var propertyName = property.Key;

                if (property.Value is not JObject propertyValue) continue;

                if (propertyValue.ContainsKey("example"))
                {
                    // Use the example value instead of the property name as the placeholder
                    newProperties[propertyName] = propertyValue["example"];
                }
                else if (propertyValue.ContainsKey("enum"))
                {
                    // Use the first value from the enum array
                    if (propertyValue["enum"] is JArray enumArray && enumArray.Count > 0)
                    {
                        newProperties[propertyName] = enumArray.First();
                    }
                }
                else if (propertyValue.ContainsKey("type"))
                {
                    newProperties[propertyName] = GetPlaceholderForType(propertyValue);
                }
                else if (propertyValue.ContainsKey("oneOf"))
                {
                    // Use only the first object from oneOf
                    if (propertyValue["oneOf"] is JArray oneOfArray && oneOfArray.First is JObject firstSchema)
                    {
                        newProperties[propertyName] = StripJsonDownToRequestObject(firstSchema);
                    }
                }
            }

            return newProperties;
        }

        /// <summary>
        /// Processes a property with a "type" field and returns an appropriate placeholder.
        /// </summary>
        /// <param name="propertyValue">The value of the property (JObject).</param>
        /// <returns>A JToken representing the placeholder based on the type.</returns>
        private static JToken? GetPlaceholderForType(JObject propertyValue)
        {
            var type = propertyValue["type"]?.ToString();

            // Return appropriate placeholder based on the type
            return type switch
            {
                "string" => "string",   // Placeholder for strings
                "integer" => 0,         // Placeholder for integers
                "number" => 0.0,        // Placeholder for numbers
                "boolean" => true,      // Placeholder for booleans
                "array" => new JArray(),// Placeholder for arrays
                "object" => StripJsonDownToRequestObject(propertyValue), // Recursively process objects
                _ => null               // If type is not recognized, return null
            };
        }
    }
}
