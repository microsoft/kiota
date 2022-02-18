<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication;


use Microsoft\Kiota\Abstractions\Authentication\BaseBearerTokenAuthenticationProvider;
use Microsoft\Kiota\Authentication\Oauth\TokenRequestContext;

/**
 * Class PhpLeagueAuthenticationProvider
 *
 * Authenticate requests using PHP League's providers to fetch the access token
 *
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class PhpLeagueAuthenticationProvider extends BaseBearerTokenAuthenticationProvider
{
    /**
     * @param TokenRequestContext $tokenRequestContext
     * @param array $scopes
     * @param array $allowedHosts
     */
    public function __construct(TokenRequestContext $tokenRequestContext, array $scopes, array $allowedHosts = [])
    {
        parent::__construct(new PhpLeagueAccessTokenProvider($tokenRequestContext, $scopes, $allowedHosts));
    }

}
