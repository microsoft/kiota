
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
        var requestSchema = operation.GetRequestSchema(validContentTypes);
        var responseSchema = operation.GetResponseSchema(validContentTypes);
        if (requestSchema != null) {
            if (!requestSchema.IsObject())
                context.CreateWarning(nameof(UrlFormEncodedComplex), $"The operation {operation.OperationId} has a request body which is not an object type. This is not supported by Kiota and serialization will fail.");
            if (requestSchema.Properties.Any(static x => x.Value.IsObject() || x.Value.IsArray()))
                context.CreateWarning(nameof(UrlFormEncodedComplex), $"The operation {operation.OperationId} has a request body with a complex properties and the url form encoded content type. This is not supported by Kiota and serialization of complex properties will fail.");
        }
        if(responseSchema != null) {
            if (!responseSchema.IsObject())
                context.CreateWarning(nameof(UrlFormEncodedComplex), $"The operation {operation.OperationId} has a response body which is not an object type. This is not supported by Kiota and deserialization will fail.");
            if (responseSchema.Properties.Any(static x => x.Value.IsObject() || x.Value.IsArray()))
               context.CreateWarning(nameof(UrlFormEncodedComplex), $"The operation {operation.OperationId} has a response body with a complex properties and the url form encoded content type. This is not supported by Kiota and deserialization of complex properties will fail.");
        }
    })
    {
    }
}
