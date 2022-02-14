<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication;

/**
 * Class ClientCredentialSecretContext
 *
 * Parameters for the Client Credentials OAuth 2.0 flow
 *
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class ClientCredentialSecretContext implements TokenRequestContext
{
    /**
     * @var string Tenant Id
     */
    private string $tenantId;
    /**
     * @var string Client Id
     */
    private string $clientId;
    /**
     * @var string Client Secret
     */
    private string $clientSecret;
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
        $this->tenantId = $tenantId;
        $this->clientId = $clientId;
        $this->clientSecret = $clientSecret;
        $this->additionalParams = $additionalParams;
    }

    /**
     * Request body parameters for client_credentials flow
     *
     * @return array<string, string>
     */
    public function getParams(): array
    {
        return array_merge($this->additionalParams, [
            'client_id' => $this->clientId,
            'client_secret' => $this->clientSecret,
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

    /**
     * Returns the tenant Id
     * @return string
     */
    public function getTenantId(): string
    {
        return $this->tenantId;
    }
}
