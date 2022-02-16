<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication;

/**
 * Class ResourceOwnerPasswordCredentialSecretContext
 *
 * Params for the Resource Owner Password Credentials flow
 *
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class ResourceOwnerPasswordCredentialSecretContext implements TokenRequestContext
{
    /**
     * @var string
     */
    private string $tenantId;
    /**
     * @var string
     */
    private string $clientId;
    /**
     * @var string
     */
    private string $clientSecret;
    /**
     * @var string
     */
    private string $username;
    /**
     * @var string
     */
    private string $password;
    /**
     * @var array
     */
    private array $additionalParams;

    /**
     * @param string $tenantId
     * @param string $clientId
     * @param string $clientSecret
     * @param string $username
     * @param string $password
     * @param array $additionalParams
     */
    public function __construct(string $tenantId, string $clientId, string $clientSecret, string $username, string $password, array $additionalParams = [])
    {
        if (!$tenantId || !$clientId || !$clientSecret || !$username || !$password) {
            throw new \InvalidArgumentException("Params cannot be empty");
        }
        $this->tenantId = $tenantId;
        $this->clientId = $clientId;
        $this->clientSecret = $clientSecret;
        $this->username = $username;
        $this->password = $password;
        $this->additionalParams = $additionalParams;
    }

    /**
     * @inheritDoc
     */
    public function getParams(): array
    {
        return array_merge($this->additionalParams, [
            'client_id' => $this->clientId,
            'client_secret' => $this->clientSecret,
            'username' => $this->username,
            'password' => $this->password,
            'grant_type' => $this->getGrantType()
        ]);
    }

    /**
     * @inheritDoc
     */
    public function getRefreshTokenParams(string $refreshToken): array
    {
        return [
            'client_id' => $this->clientId,
            'client_secret' => $this->clientSecret,
            'refresh_token' => $refreshToken,
            'grant_type' => 'refresh_token'
        ];
    }

    /**
     * @inheritDoc
     */
    public function getGrantType(): string
    {
        return 'password';
    }

    /**
     * @inheritDoc
     */
    public function getTenantId(): string
    {
        return $this->tenantId;
    }
}
