// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions.Authentication
{
    /// <summary>
    /// This authentication provider does not perform any authentication.
    /// </summary>
    public class AnonymousAuthenticationProvider : IAuthenticationProvider
    {
        /// <summary>
        /// Authenticates the <see cref="RequestInformation"/> instance
        /// </summary>
        /// <param name="request">The request to authenticate</param>
        /// <param name="cancellationToken">The cancellation token for the task</param>
        /// <returns></returns>
        public Task AuthenticateRequestAsync(RequestInformation request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
