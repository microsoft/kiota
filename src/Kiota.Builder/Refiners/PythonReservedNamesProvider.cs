using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class PythonReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(() => new(StringComparer.OrdinalIgnoreCase) {
        "and",
        "as",
        "assert",
        "async",
        "await",
        "break",
        "class",
        "continue",
        "def",
        "del",
        "dict",
        "elif",
        "else",
        "except",
        "finally",
        "False",
        "for",
        "from",
        "global",
        "if",
        "import",
        "in",
        "is",
        "lambda",
        "list",
        "nonlocal",
        "None",
        "not",
        "or",
        "pass",
        "raise",
        "return",
        "True",
        "try",
        "with",
        "while",
        "yield",
        "property",
        "BaseRequestBuilder",
        "MultipartBody",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
