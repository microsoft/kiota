using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class JavaExceptionsReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "addSuppressed",
        "clone",
        "equals",
        "fillInStackTrace",
        "finalize",
        "getCause",
        "getClass",
        "getLocalizedMessage",
        "getMessage",
        "getStackTrace",
        "getSuppressed",
        "hashCode",
        "initCause",
        "notify",
        "notifyAll",
        "printStackTrace",
        "setStackTrace",
        "toString",
        "ResponseStatusCode",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
