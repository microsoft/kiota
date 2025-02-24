﻿using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;

public class GetWithBody : ValidationRule<IOpenApiPathItem>
{
    public GetWithBody() : base(nameof(GetWithBody), static (context, pathItem) =>
    {
        if (pathItem.Operations.TryGetValue(OperationType.Get, out var getOperation) && getOperation.RequestBody != null)
            context.CreateWarning(nameof(GetWithBody), "A GET operation with a body was found. The request body will be ignored.");
    })
    {
    }
}
