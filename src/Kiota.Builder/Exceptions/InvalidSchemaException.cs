
using System;

namespace Kiota.Builder.Exceptions;

[Serializable]
public class InvalidSchemaException : InvalidOperationException
{
    public InvalidSchemaException():base(){}
    public InvalidSchemaException(string message) : base(message){}
    #nullable enable
    public InvalidSchemaException(string? message, Exception? innerException):base(message, innerException){}
    #nullable disable
}
