using System;

namespace Kiota.Builder.Plugins;

public class UnsupportedSecuritySchemeException(string[] supportedTypes, string? message, Exception? innerException)
    : Exception(message, innerException)
{
#pragma warning disable CA1819
    public string[] SupportedTypes => supportedTypes;
#pragma warning restore CA1819

    public UnsupportedSecuritySchemeException(string[] supportedTypes, string? message) : this(supportedTypes, message,
        null)
    {
    }

    public UnsupportedSecuritySchemeException() : this(null)
    {
    }

    public UnsupportedSecuritySchemeException(string? message) : this(message, null)
    {
    }

    public UnsupportedSecuritySchemeException(string? message, Exception? innerException) : this([], message, innerException)
    {
    }
}
