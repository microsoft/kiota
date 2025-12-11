using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class PythonExceptionsReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "mro",
        "with_traceback",
        "response_status_code",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
