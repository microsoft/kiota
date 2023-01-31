using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions;
public static class OpenApiReferenceExtensions
{
    public static string GetClassName(this OpenApiReference? reference)
    {
        var referenceId = reference?.Id ?? string.Empty;
        return referenceId[(referenceId.LastIndexOf('.') + 1)..]
                                    .ToFirstCharacterUpperCase();
    }
}
