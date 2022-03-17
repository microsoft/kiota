using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions {
    public static class OpenApiOperationExtensions {
        private static readonly HashSet<string> successCodes = new() {"200", "201", "202"}; //204 excluded as it won't have a schema
        private static HashSet<string> validMimeTypes = new (StringComparer.OrdinalIgnoreCase) {
            "application/json",
            "text/plain"
        };
        public static OpenApiSchema GetResponseSchema(this OpenApiOperation operation)
        {
            // Return Schema that represents all the possible success responses!
            // For the moment assume 200s and application/json
            var schemas = operation.Responses.Where(r => successCodes.Contains(r.Key))
                                .SelectMany(re => re.Value.Content)
                                .Where(c => validMimeTypes.Contains(c.Key))
                                .Select(co => co.Value.Schema)
                                .Where(s => s is not null);

            return schemas.FirstOrDefault();
        }
        public static OpenApiSchema GetResponseSchema(this OpenApiResponse response)
        {
            // For the moment assume application/json
            var schemas = response.Content
                                .Where(c => validMimeTypes.Contains(c.Key))
                                .Select(co => co.Value.Schema)
                                .Where(s => s is not null);

            return schemas.FirstOrDefault();
        }
    }
}
