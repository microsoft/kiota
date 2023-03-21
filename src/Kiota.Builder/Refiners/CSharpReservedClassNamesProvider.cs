using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class CSharpReservedClassNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "BaseRequestBuilder",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
