using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class TypeScriptExceptionsReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "cause",
        "columnNumber",
        "fileName",
        "lineNumber",
        "message",
        "name",
        "stack",
        "toString",
        "ResponseStatusCode",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
