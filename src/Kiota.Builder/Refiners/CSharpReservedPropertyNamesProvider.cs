using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

/// <summary>
/// Names that are not allowed for properties.
/// </summary>
public class CSharpReservedPropertyNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "Equals",  //warning "hides inherited member 'object.Equals(object?)'", https://github.com/microsoft/kiota/issues/8133
        "GetHashCode",
        "ToString",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
