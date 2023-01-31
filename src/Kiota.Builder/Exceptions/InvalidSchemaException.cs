
using System;

namespace Kiota.Builder.Exceptions;

internal class InvalidSchemaException : InvalidOperationException
{
    public InvalidSchemaException()
    {
    }
    public InvalidSchemaException(string message) : base(message) { }
#nullable enable
    public InvalidSchemaException(string? message, Exception? innerException) : base(message, innerException) { }
#nullable disable
}
