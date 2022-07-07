
using System;

namespace Kiota.Builder.Exceptions;

public class InvalidSchemaException : InvalidOperationException
{
    public InvalidSchemaException(string message) : base(message)
    {
    }
}
