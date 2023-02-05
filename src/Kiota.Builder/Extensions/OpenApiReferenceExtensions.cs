using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions;
public static class OpenApiReferenceExtensions
{
    public static string GetClassName(this OpenApiReference? reference)
    {
        if (reference?.Id is string referenceId && !string.IsNullOrEmpty(referenceId))
            return referenceId[(referenceId.LastIndexOf('.') + 1)..]
                                    .ToFirstCharacterUpperCase();
        return string.Empty;
    }
}
