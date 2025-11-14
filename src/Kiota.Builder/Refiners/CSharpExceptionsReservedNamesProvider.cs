using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class CSharpExceptionsReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "data",
        "helpLink",
        "hResult",
        "innerException",
        "message",
        "source",
        "stacktrace",
        "targetSite",
        "GetBaseException",
        "GetObjectData",
        "GetType",
        "ToString",
        "ResponseStatusCode",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
