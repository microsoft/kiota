using System.Linq;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder {
    public static class OpenApiOperationExtensions {
        public static OpenApiSchema GetResponseSchema(this OpenApiOperation operation)
        {
            // Return Schema that represents all the possible success responses!
            // For the moment assume 200s and application/json
            // TODO: figure out how to create types that accurately correspond to HTTP responses!
            var schemas = operation.Responses.Where(r => r.Key == "200" || r.Key == "201")
                                .SelectMany(re => re.Value.Content)
                                .Where(c => c.Key == "application/json")
                                .Select(co => co.Value.Schema)
                                .Where(s => s is not null);

            return schemas.FirstOrDefault();
        }
    }
}
