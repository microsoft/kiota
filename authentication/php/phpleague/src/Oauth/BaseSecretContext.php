<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication\Oauth;

/**
 * Class BaseSecretContext
 *
 * Defines common functionality for all secret-based auth flows
 *
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class BaseSecretContext implements TokenRequestContext
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
     * @param string $tenantId
     * @param string $clientId
     * @param string $clientSecret
     */
    public function __construct(string $tenantId, string $clientId, string $clientSecret)
    {
        if (!$tenantId || !$clientId || !$clientSecret) {
            throw new \InvalidArgumentException("TenantId, clientId and clientSecret cannot be empty");
        }
        $this->tenantId = $tenantId;
        $this->clientId = $clientId;
        $this->clientSecret = $clientSecret;
    }

    /**
     * @inheritDoc
     */
    public function getParams(): array
    {
        return [
            'client_id' => $this->clientId,
            'client_secret' => $this->clientSecret
        ];
    }

    public function getRefreshTokenParams(string $refreshToken): array
    {
        return [
            'client_id' => $this->clientId,
            'client_secret' => $this->clientSecret,
            'refresh_token' => $refreshToken,
            'grant_type' => 'refresh_token'
        ];
    }

    public function getGrantType(): string
    {
        return '';
    }

    public function getTenantId(): string
    {
        return $this->tenantId;
    }
}
