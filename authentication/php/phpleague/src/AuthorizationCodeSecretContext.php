<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication;

/**
 * Class AuthorizationCodeSecretContext
 *
 * Request params for the token request of the authorization_code flow using a secret
 *
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class AuthorizationCodeSecretContext implements TokenRequestContext
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
     * @var string Code from the authorization step
     */
    private string $authCode;
    /**
     * @var string Same redirectUri used to acquire the authorization code
     */
    private string $redirectUri;
    /**
     * @var array Extra params to add to the request
     */
    private array $additionalParams;

    /**
     * @param string $tenantId
     * @param string $clientId
     * @param string $clientSecret
     * @param string $authCode
     * @param string $redirectUri
     * @param array $additionalParams
     */
    public function __construct(string $tenantId, string $clientId, string $clientSecret, string $authCode, string $redirectUri, array $additionalParams = [])
    {
        if (!$tenantId || !$clientId || !$clientSecret || !$authCode || !$redirectUri) {
            throw new \InvalidArgumentException("Params cannot be empty");
        }
        $this->tenantId = $tenantId;
        $this->clientId = $clientId;
        $this->clientSecret = $clientSecret;
        $this->authCode = $authCode;
        $this->redirectUri = $redirectUri;
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
            'code' => $this->authCode,
            'redirect_uri' => $this->redirectUri,
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
        return 'authorization_code';
    }

    /**
     * @inheritDoc
     */
    public function getTenantId(): string
    {
        return $this->tenantId;
    }
}
