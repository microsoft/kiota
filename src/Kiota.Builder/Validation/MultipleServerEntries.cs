using System;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;
public class MultipleServerEntries : ValidationRule<OpenApiDocument>
{
    public MultipleServerEntries() : base(static (context, document) =>
    {
        if (document.Servers.GroupBy(static x => x.Url, StringComparer.OrdinalIgnoreCase).Count() > 1)
            context.CreateWarning(nameof(MultipleServerEntries),
                                "Multiple servers entries were found in the OpenAPI description. Only the first one will be used. The root URL can be set manually with the request adapter.");
    })
    {
    }
}
