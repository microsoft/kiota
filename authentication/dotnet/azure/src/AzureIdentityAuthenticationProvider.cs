// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Microsoft.Kiota.Authentication.Azure;
/// <summary>
/// The <see cref="BaseBearerTokenAuthenticationProvider"/> implementation that supports implementations of <see cref="TokenCredential"/> from Azure.Identity.
/// </summary>
public class AzureIdentityAuthenticationProvider : BaseBearerTokenAuthenticationProvider
{
    /// <summary>
    /// The <see cref="AzureIdentityAuthenticationProvider"/> constructor
    /// </summary>
    /// <param name="credential">The credential implementation to use to obtain the access token.</param>
    /// <param name="scopes">The scopes to request the access token for.</param>
    public AzureIdentityAuthenticationProvider(TokenCredential credential, params string[] scopes) : base(new AzureIdentityAccessTokenProvider(credential, scopes))
    {
    }
}
