using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class PhpExceptionsReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "message",
        "code",
        "file",
        "line",
        "response",
        "responseStatusCode",
        "getMessage",
        "getPrevious",
        "getCode",
        "getFile",
        "getLine",
        "getTrace",
        "getTraceAsString",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
