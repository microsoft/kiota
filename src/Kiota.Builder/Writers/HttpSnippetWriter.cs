using System;
using System.Collections.Generic;
using System.IO;
using Kiota.Builder.Writers.Go;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace Kiota.Builder.Writers
{
    internal class HttpSnippetWriter
    {
        /// <summary>
        /// The text writer.
        /// </summary>
        protected TextWriter Writer
        {
            get;
        }

        public HttpSnippetWriter(TextWriter writer) 
        { 
            Writer = writer;
        }

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

                Writer.WriteLine($"### {operation} {path}");
                // write the parameters 
                WriteParameters(item.Value.Parameters);
                Writer.WriteLine($"{operation} {{{{url}}}}{path} HTTP/1.1");
                Writer.WriteLine();
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

    }
}
