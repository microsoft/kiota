using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kiota.Builder.Writers.http
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

        // Create OpenApiWriterSettings with InlineReferencedSchemas set to true
        private static readonly OpenApiWriterSettings _settings = new()
        {
            InlineLocalReferences = true,
            InlineExternalReferences = true,
        };

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
        /// Each path item is processed by calling the <see cref="WriteOpenApiPathItem"/> method.
        /// </summary>
        /// <param name="node">The OpenAPI URL tree node containing the path items to write.</param>
        private void WritePathItems(OpenApiUrlTreeNode node)
        {
            // Write all the path items
            foreach (var item in node.PathItems)
            {
                WriteOpenApiPathItem(item.Value, node.Path);
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
        public void WriteOpenApiPathItem(OpenApiPathItem pathItem, string path)
        {
            // Sanitize the path element
            path = SanitizePath(path);

            // Write the operation
            foreach (var item in pathItem.Operations)
            {
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

                var schema = content.Value.Schema;
                if (schema == null) return;

                var json = ConvertToJson(schema);
                JObject jsonSchema = JsonHelper.StripJsonDownToRequestObject(json);
                Writer.WriteLine(jsonSchema.ToString(Formatting.Indented));
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
                var parameterJsonObject = ConvertToJson(parameter);
                var name = parameterJsonObject["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name)) continue;
                Writer.WriteLine($"# {parameterJsonObject["description"]?.ToString()}");
                Writer.WriteLine($"@{name} = {parameterJsonObject["example"]?.ToString()}");
            }
        }

        /// <summary>
        /// Flush the writer.
        /// </summary>
        public void Flush()
        {
            Writer.Flush();
        }

        private static JObject ConvertToJson(IOpenApiReferenceable schema)
        {
            using var stringWriter = new StringWriter();
            var jsonWriter = new OpenApiJsonWriter(stringWriter, _settings);
            schema.SerializeAsV3WithoutReference(jsonWriter);
            // Return the resulting JSON
            return JObject.Parse(stringWriter.ToString());
        }
    }
}
