using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class RustExceptionsReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.Ordinal)
    {
        "to_string",
        "fmt",
        "source",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
