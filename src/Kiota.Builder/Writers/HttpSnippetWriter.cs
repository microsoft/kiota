using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kiota.Builder.Writers
{
    internal class HttpSnippetWriter(TextWriter writer)
    {
        /// <summary>
        /// The text writer.
        /// </summary>
        protected TextWriter Writer
        {
            get;
        } = writer;

        /// <summary>
        /// Writes the given OpenAPI URL tree node to the writer.
        /// This includes writing all path items and their children.
        /// </summary>
        /// <param name="node">The OpenAPI URL tree node to write.</param>
        public void Write(OpenApiUrlTreeNode node)
        {
            WritePathItems(node);
            WriteChildren(node);
        }

        /// <summary>
        /// Writes all the path items for the given OpenAPI URL tree node to the writer.
        /// Each path item is processed by calling the <see cref="WriteOpenApiPathItemOperation"/> method.
        /// </summary>
        /// <param name="node">The OpenAPI URL tree node containing the path items to write.</param>
        private void WritePathItems(OpenApiUrlTreeNode node)
        {
            // Write all the path items
            foreach (var item in node.PathItems)
            {
                WriteOpenApiPathItemOperation(item.Value, node);
            }
        }

        /// <summary>
        /// Writes the children of the given OpenAPI URL tree node to the writer.
        /// Each child node is processed by calling the <see cref="Write"/> method.
        /// </summary>
        /// <param name="node">The OpenAPI URL tree node whose children are to be written.</param>
        private void WriteChildren(OpenApiUrlTreeNode node)
        {
            foreach (var item in node.Children)
            {
                Write(item.Value);
            }
        }

        /// <summary>
        /// Writes the operations for the given OpenAPI path item to the writer.
        /// Each operation includes the HTTP method, sanitized path, parameters, and a formatted HTTP request line.
        /// </summary>
        /// <param name="pathItem">The OpenAPI path item containing the operations to write.</param>
        /// <param name="node">The OpenAPI URL tree node representing the path.</param>
        private void WriteOpenApiPathItemOperation(OpenApiPathItem pathItem, OpenApiUrlTreeNode node)
        {
            // Write the operation
            foreach (var item in pathItem.Operations)
            {
                var path = SanitizePath(node.Path);
                var operation = item.Key.ToString().ToUpperInvariant();

                // write the comment which also acts as the sections delimiter 
                Writer.WriteLine($"### {operation} {path}");

                // write the parameters 
                WriteParameters(item.Value.Parameters);

                // write the http request operation 
                Writer.WriteLine($"{operation} {{{{url}}}}{path} HTTP/1.1");

                // Write the request body if any
                WriteRequestBody(item.Value.RequestBody);

                Writer.WriteLine();
            }
        }

        private void WriteRequestBody(OpenApiRequestBody requestBody)
        {
            if (requestBody == null) return;

            foreach (var content in requestBody.Content)
            {
                // Write content type
                Writer.WriteLine("Content-Type: " + content.Key);

                // If example exist use it
                if (content.Value.Example != null)
                {
                    Writer.WriteLine(content.Value.Example);
                }
                else
                {
                    var schema = content.Value.Schema;
                    if (schema == null) return;

                    var schemaString = ConvertSchemaToJsonString(schema);
                    JObject parsedJson = JObject.Parse(schemaString);
                    JObject strippedJson = StripJsonDownToRequestObject(parsedJson);
                    Writer.WriteLine(strippedJson.ToString(Formatting.Indented));
                }
            }
        }

        /// <summary>
        /// Sanitizes the given path by replacing '\\' with '/' and '\' with '/'.
        /// Also converts '{foo}' into '{{foo}}' so that they can be used as variables in the HTTP snippet.
        /// </summary>
        /// <param name="path">The path to sanitize.</param>
        /// <returns>The sanitized path.</returns>
        private static string SanitizePath(string path)
        {
            return path.Replace("\\\\", "/", StringComparison.OrdinalIgnoreCase)
                    .Replace("\\", "/", StringComparison.OrdinalIgnoreCase)
                    .Replace("{", "{{", StringComparison.OrdinalIgnoreCase)
                    .Replace("}", "}}", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Writes the given list of OpenAPI parameters to the writer.
        /// Each parameter's description and example value are written as comments and variable assignments, respectively.
        /// </summary>
        /// <param name="parameters">The list of OpenAPI parameters to write.</param>
        private void WriteParameters(IList<OpenApiParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                Writer.WriteLine($"# {parameter.Description}");
                Writer.WriteLine($"@{parameter.Name} = {parameter.Example}");
            }
        }

        /// <summary>
        /// Flush the writer.
        /// </summary>
        public void Flush()
        {
            Writer.Flush();
        }

        private static string ConvertSchemaToJsonString(OpenApiSchema schema)
        {
            using (var stringWriter = new StringWriter())
            {
                // Create OpenApiWriterSettings with InlineReferencedSchemas set to true
                var settings = new OpenApiWriterSettings
                {
                    InlineLocalReferences = true,
                    InlineExternalReferences = true, // This will inline the references
                };

                var jsonWriter = new OpenApiJsonWriter(stringWriter, settings);

                schema.SerializeAsV3WithoutReference(jsonWriter);

                // Return the resulting JSON string
                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Recursively processes a JSON schema to remove `type` properties and replaces them with placeholders.
        /// Handles primitive types (`string`, `integer`, `number`, `boolean`, `null`), arrays, objects, 
        /// and special schema keywords (`enum`, `const`, `oneOf`, `anyOf`, `allOf`).
        /// </summary>
        /// <param name="obj">The JSON object (JObject) representing a schema.</param>
        /// <returns>A new JObject with `type` properties stripped and replaced with placeholders.</returns>
        private static JObject StripJsonDownToRequestObject(JObject obj)
        {
            if (obj.ContainsKey("properties"))
            {
                var propertiesObject = obj["properties"];

                if (propertiesObject == null) return [];

                JObject properties = (JObject)propertiesObject;

                var newProperties = new JObject();

                foreach (var property in properties)
                {
                    var propertyName = property.Key;

                    if (property.Value is JObject propertyValue)
                    {
                        if (propertyValue.ContainsKey("type"))
                        {
                            var type = propertyValue["type"]?.ToString();

                            // Handle primitive types
                            if (type == "string" || type == "integer" || type == "number" || type == "boolean" || type == "null")
                            {
                                newProperties[propertyName] = propertyName;
                            }
                            else if (type == "object" && propertyValue.ContainsKey("properties"))
                            {
                                // Recursively handle child objects
                                newProperties[propertyName] = StripJsonDownToRequestObject(propertyValue);
                            }
                            else if (type == "array" && propertyValue.ContainsKey("items"))
                            {
                                if (propertyValue["items"] is JObject items && items.ContainsKey("type"))
                                {
                                    var itemType = items["type"]?.ToString();

                                    if (itemType == "string" || itemType == "integer" || itemType == "number" || itemType == "boolean" || itemType == "null")
                                    {
                                        newProperties[propertyName] = new JArray(propertyName); // Replace array with property name placeholder
                                    }
                                    else if (itemType == "object" && items.ContainsKey("properties"))
                                    {
                                        // Recursively process the object items within the array
                                        newProperties[propertyName] = new JArray(StripJsonDownToRequestObject(items));
                                    }
                                }
                            }
                        }
                        else if (propertyValue.ContainsKey("enum"))
                        {
                            // Handle enums, replace with property name
                            newProperties[propertyName] = propertyName;
                        }
                        else if (propertyValue.ContainsKey("const"))
                        {
                            // Handle const, replace with property name
                            newProperties[propertyName] = propertyName;
                        }
                        else if (propertyValue.ContainsKey("oneOf") || propertyValue.ContainsKey("anyOf") || propertyValue.ContainsKey("allOf"))
                        {
                            // Handle complex validation keywords like oneOf, anyOf, allOf
                            newProperties[propertyName] = propertyName;
                        }
                    }
                }

                return newProperties;
            }

            return obj;
        }
    }
}
