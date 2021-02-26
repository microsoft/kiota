using Microsoft.OpenApi.Models;

namespace Kiota.Builder {
    public static class OpenApiReferenceExtensions {
        public static string GetClassName(this OpenApiReference reference) {
            var referenceId = reference?.Id;
            return referenceId?.Substring((referenceId?.LastIndexOf('.') ?? 0) + 1)
                                          ?.ToFirstCharacterUpperCase();
        }
    }
}
