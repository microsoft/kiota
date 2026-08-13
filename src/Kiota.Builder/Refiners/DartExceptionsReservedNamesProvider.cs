using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class DartExceptionsReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "toString"
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
