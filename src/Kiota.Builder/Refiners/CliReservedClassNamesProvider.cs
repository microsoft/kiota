using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;
public class CliReservedClassNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "BaseRequestBuilder",
        "BaseCliRequestBuilder",
        "Command",
        "Option",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
