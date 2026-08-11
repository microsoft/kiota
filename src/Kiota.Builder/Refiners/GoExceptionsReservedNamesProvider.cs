using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class GoExceptionsReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "error",
        "ResponseStatusCode",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
