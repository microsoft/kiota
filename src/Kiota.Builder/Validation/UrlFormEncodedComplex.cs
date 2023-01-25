
using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;
public class UrlFormEncodedComplex : ValidationRule<OpenApiOperation>
{
    private static readonly HashSet<string> validContentTypes = new(StringComparer.OrdinalIgnoreCase) {
        "application/x-www-form-urlencoded",
    };
    public UrlFormEncodedComplex() : base(static (context, operation) => {
        if (operation.GetRequestSchema(validContentTypes) is OpenApiSchema requestSchema)
            ValidateSchema(requestSchema, context, operation.OperationId, "request body");
        if (operation.GetResponseSchema(validContentTypes) is OpenApiSchema responseSchema)
            ValidateSchema(responseSchema, context, operation.OperationId, "response body");
    })
    {
    }
    private static void ValidateSchema(OpenApiSchema schema, IValidationContext context, string operationId, string schemaName) {
        if(schema == null) return;
        if (!schema.IsObject())
            context.CreateWarning(nameof(UrlFormEncodedComplex), $"The operation {operationId} has a {schemaName} which is not an object type. This is not supported by Kiota and serialization will fail.");
        if (schema.Properties.Any(static x => x.Value.IsObject() || x.Value.IsArray()))
            context.CreateWarning(nameof(UrlFormEncodedComplex), $"The operation {operationId} has a {schemaName} with a complex properties and the url form encoded content type. This is not supported by Kiota and serialization of complex properties will fail.");
    }
}
