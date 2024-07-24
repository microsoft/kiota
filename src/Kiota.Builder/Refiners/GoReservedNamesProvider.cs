using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class GoReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(() =>
    {
        var reservedNames = new GoNamespaceReservedNamesProvider().ReservedNames;
        reservedNames.Add("go");
        return reservedNames;
    });

    public HashSet<string> ReservedNames => _reservedNames.Value;
}
