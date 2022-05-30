using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions {
    public static class OpenApiOperationExtensions {
        private static readonly HashSet<string> successCodes = new(StringComparer.OrdinalIgnoreCase) {"200", "201", "202"}; //204 excluded as it won't have a schema
        /// <summary>
        /// cleans application/vnd.github.mercy-preview+json to application/json
        /// </summary>
        private static readonly Regex vendorSpecificCleanup = new(@"[^/]+\+", RegexOptions.Compiled);
        public static OpenApiSchema GetResponseSchema(this OpenApiOperation operation, HashSet<string> structuredMimeTypes)
        {
            // Return Schema that represents all the possible success responses!
            var schemas = operation.Responses.Where(r => successCodes.Contains(r.Key))
                                .SelectMany(re => re.Value.Content.GetValidSchemas(structuredMimeTypes));

            return schemas.FirstOrDefault();
        }
        public static IEnumerable<OpenApiSchema> GetValidSchemas(this IDictionary<string, OpenApiMediaType> source, HashSet<string> structuredMimeTypes)
        {
            if(!(structuredMimeTypes?.Any() ?? true))
                throw new ArgumentNullException(nameof(structuredMimeTypes));
            var schemas = source
                                .Where(c => !string.IsNullOrEmpty(c.Key))
                                .Select(c => (Key: c.Key.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), c.Value))
                                .Where(c => structuredMimeTypes.Contains(c.Key) || structuredMimeTypes.Contains(vendorSpecificCleanup.Replace(c.Key, string.Empty)))
                                .Select(co => co.Value.Schema)
                                .Where(s => s is not null);

            return schemas;
        }
        public static OpenApiSchema GetResponseSchema(this OpenApiResponse response, HashSet<string> structuredMimeTypes)
        {
            return response.Content.GetValidSchemas(structuredMimeTypes).FirstOrDefault();
        }
    }
}
