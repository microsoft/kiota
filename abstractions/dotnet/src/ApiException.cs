// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------
using System;

namespace Microsoft.Kiota.Abstractions;

/// <summary>
/// Parent type for exceptions thrown by the client when receiving failed responses to its requests.
/// </summary>
public class ApiException : Exception
{
    /// <inheritdoc/>
    public ApiException(): base()
    {
        
    }
    /// <inheritdoc/>
    public ApiException(string message) : base(message)
    {
    }
    /// <inheritdoc/>
    public ApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
