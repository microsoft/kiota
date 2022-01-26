<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Abstractions\Authentication;

use Http\Promise\Promise;

/**
 * Interface AccessTokenProvider
 *
 * Defines a contract for obtaining access tokens for a given url
 *
 * @package Microsoft\Kiota\Abstractions\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
interface AccessTokenProvider
{
    /**
     * Method called by {@link BaseBearerTokenAuthenticationProvider} to get the access token
     * Returns a Promise that is fulfilled with the access token string
     *
     * @param string $url the target URI to get an access token for
     * @return Promise
     */
    public function getAuthorizationTokenAsync(string $url): Promise;
}