using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions {
    public static class OpenApiOperationExtensions {
        internal static readonly HashSet<string> SuccessCodes = new(StringComparer.OrdinalIgnoreCase) {"200", "201", "202", "203", "2XX"}; //204 excluded as it won't have a schema
        /// <summary>
        /// cleans application/vnd.github.mercy-preview+json to application/json
        /// </summary>
        private static readonly Regex vendorSpecificCleanup = new(@"[^/]+\+", RegexOptions.Compiled);
        public static OpenApiSchema GetResponseSchema(this OpenApiOperation operation, HashSet<string> structuredMimeTypes)
        {
            // Return Schema that represents all the possible success responses!
            return operation.GetResponseSchemas(SuccessCodes, structuredMimeTypes)
                                .FirstOrDefault();
        }
        internal static IEnumerable<OpenApiSchema> GetResponseSchemas(this OpenApiOperation operation, HashSet<string> successCodesToUse, HashSet<string> structuredMimeTypes)
        {
            // Return Schema that represents all the possible success responses!
            return operation.Responses.Where(r => successCodesToUse.Contains(r.Key))
                                .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                                .SelectMany(re => re.Value.Content.GetValidSchemas(structuredMimeTypes));
        }
        public static OpenApiSchema GetRequestSchema(this OpenApiOperation operation, HashSet<string> structuredMimeTypes)
        {
            return operation.RequestBody?.Content
                                .GetValidSchemas(structuredMimeTypes).FirstOrDefault();
        }
        public static IEnumerable<OpenApiSchema> GetValidSchemas(this IDictionary<string, OpenApiMediaType> source, HashSet<string> structuredMimeTypes)
        {
            if(!(structuredMimeTypes?.Any() ?? false))
                throw new ArgumentNullException(nameof(structuredMimeTypes));
            return source?
                                .Where(static c => !string.IsNullOrEmpty(c.Key))
                                .Select(static c => (Key: c.Key.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), c.Value))
                                .Where(c => structuredMimeTypes.Contains(c.Key) || structuredMimeTypes.Contains(vendorSpecificCleanup.Replace(c.Key, string.Empty)))
                                .Select(static co => co.Value.Schema)
                                .Where(static s => s is not null) ??
                            Enumerable.Empty<OpenApiSchema>();
        }
        public static OpenApiSchema GetResponseSchema(this OpenApiResponse response, HashSet<string> structuredMimeTypes)
        {
            return response.Content.GetValidSchemas(structuredMimeTypes).FirstOrDefault();
        }
    }
}
