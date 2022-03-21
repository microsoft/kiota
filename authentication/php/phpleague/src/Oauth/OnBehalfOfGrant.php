<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication\Oauth;


use League\OAuth2\Client\Grant\AbstractGrant;

/**
 * Class OnBehalfOfGrant
 *
 * Grant type for on_behalf_of flows
 *
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class OnBehalfOfGrant extends AbstractGrant
{

    protected function getName(): string
    {
        return 'urn:ietf:params:Oauth:grant-type:jwt-bearer';
    }

    protected function getRequiredRequestParameters(): array
    {
        return [
            'requested_token_use',
            'client_id',
            'assertion',
            'scope'
        ];
    }
}
