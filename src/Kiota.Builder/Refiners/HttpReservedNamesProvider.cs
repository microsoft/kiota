using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class HttpReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(() => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
