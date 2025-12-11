using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class GoNamespaceReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(() => new(StringComparer.OrdinalIgnoreCase) {
        "break",
        "case",
        "chan",
        "const",
        "continue",
        "default",
        "defer",
        "else",
        "fallthrough",
        "for",
        "func",
        "goto",
        "if",
        "import",
        "interface",
        "map",
        "package",
        "range",
        "return",
        "select",
        "struct",
        "switch",
        "type",
        "var",
        "vendor", // cannot be used as a package name
        "BaseRequestBuilder",
        "MultipartBody",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
