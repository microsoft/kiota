// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions.Authentication
{
    /// <summary>
    /// Authenticates the application request.
    /// </summary>
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Authenticates the application request.
        /// </summary>
        /// <param name="request">The request to authenticate.</param>
        /// <param name="cancellationToken">The cancellation token for the task</param>
        /// <returns>A task to await for the authentication to be completed.</returns>
        Task AuthenticateRequestAsync(RequestInformation request, CancellationToken cancellationToken = default);
    }
}
