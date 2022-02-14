<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication;

use League\OAuth2\Client\Grant\AbstractGrant;

/**
 * Interface TokenRequestContext
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
interface TokenRequestContext
{
    /**
     * Return dictionary with OAuth 2.0 request parameters to be passed to PHP League's OAuth provider
     * @return array<string, string>
     */
    public function getParams(): array;

    /**
     * @return string Grant type
     */
    public function getGrantType(): string;

    /**
     * Return the tenantId
     * @return string
     */
    public function getTenantId(): string;
}
