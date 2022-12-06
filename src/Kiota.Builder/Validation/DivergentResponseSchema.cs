using System;
using System.Linq;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;
public class DivergentResponseSchema : ValidationRule<OpenApiOperation>
{
    private static readonly OpenApiSchemaComparer schemaComparer = new();
    public DivergentResponseSchema(GenerationConfiguration configuration) : base((context, operation) => {
        var schemas = operation.GetResponseSchemas(new(StringComparer.OrdinalIgnoreCase) {"200", "201", "202", "203"}, configuration.StructuredMimeTypes);
        if(schemas.GroupBy(x => x, schemaComparer).Count() > 1)
            context.CreateWarning(nameof(DivergentResponseSchema), "The operation describes multiple response schemas that are divergent. Only the schema of the lowest success status code will be used.");
    })
    {
        ArgumentNullException.ThrowIfNull(configuration);
    }
}
