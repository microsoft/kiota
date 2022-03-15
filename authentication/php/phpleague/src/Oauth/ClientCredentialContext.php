<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication\Oauth;

/**
 * Class ClientCredentialContext
 *
 * Parameters for the client_credentials OAuth 2.0 flow using a secret
 *
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class ClientCredentialContext extends BaseSecretContext
{
    /**
     * @var array<string, string> Key-value pairs of additional OAuth 2.0 parameters
     */
    private array $additionalParams;

    /**
     * Creates a new instance
     * @param string $tenantId
     * @param string $clientId
     * @param string $clientSecret
     * @param array<string, string> $additionalParams extra OAuth 2.0 parameters for client credentials flow
     */
    public function __construct(string $tenantId, string $clientId, string $clientSecret, array $additionalParams = [])
    {
        $this->additionalParams = $additionalParams;
        parent::__construct($tenantId, $clientId, $clientSecret);
    }

    /**
     * Request body parameters for client_credentials flow
     *
     * @return array<string, string>
     */
    public function getParams(): array
    {
        return array_merge($this->additionalParams, parent::getParams(), [
            'grant_type' => $this->getGrantType(),
        ]);
    }

    /**
     * Returns the Grant type
     * @return string
     */
    public function getGrantType(): string
    {
        return 'client_credentials';
    }
}
