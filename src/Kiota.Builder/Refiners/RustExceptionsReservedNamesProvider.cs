using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class RustExceptionsReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "error",
        "source",
        "description",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
