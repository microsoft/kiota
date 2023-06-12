
using System;

namespace Kiota.Builder.Exceptions;

internal class InvalidSchemaException : InvalidOperationException
{
    public InvalidSchemaException(string message) : base(message)
    {
    }

    public InvalidSchemaException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public InvalidSchemaException()
    {
    }
}
