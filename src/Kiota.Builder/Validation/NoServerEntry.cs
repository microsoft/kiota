﻿using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;

public class NoServerEntry : ValidationRule<OpenApiDocument>
{
    public NoServerEntry() : base(nameof(NoServerEntry), static (context, document) =>
    {
        if (document.Servers is not { Count: > 0 } || string.IsNullOrEmpty(document.Servers[0].Url?.TrimEnd('/')))
            context.CreateWarning(nameof(NoServerEntry),
                                "A servers entry (v3) or host + basePath + schemes properties (v2) was not present in the OpenAPI description. The root URL will need to be set manually with the request adapter.");
    })
    {
    }
}
