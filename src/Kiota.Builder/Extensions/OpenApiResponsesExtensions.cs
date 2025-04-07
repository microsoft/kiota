using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;

namespace Kiota.Builder.Extensions;
public static class OpenApiResponsesExtensions
{
    private static readonly HashSet<string> ignorableStatusCodesForMediaTypes = new([
        ..Enumerable.Range(400, 599).Select(static x => x.ToString(CultureInfo.InvariantCulture)),
        CodeMethod.ErrorMappingClientRange,
        CodeMethod.ErrorMappingServerRange,
        "default"]);

    public static IEnumerable<KeyValuePair<string, IOpenApiResponse>> WhereValidForMediaTypeSelection(this OpenApiResponses responses)
        => responses.Where(static x => !ignorableStatusCodesForMediaTypes.Contains(x.Key) && x.Value.Content is not null);
}
