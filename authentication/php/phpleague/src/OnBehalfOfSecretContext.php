<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication;

/**
 * Class OnBehalfOfSecretContext
 *
 * Token request parameters for the on_behalf_of flow
 *
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class OnBehalfOfSecretContext implements TokenRequestContext
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
    private string $assertion;
    /**
     * @var array<string, string>
     */
    private array $additionalParams;

    /**
     * @param string $tenantId
     * @param string $clientId
     * @param string $clientSecret
     * @param string $assertion initial access token sent to the current API
     * @param array<string, string> $additionalParams extra params to be added to token request
     */
    public function __construct(string $tenantId, string $clientId, string $clientSecret, string $assertion, array $additionalParams = [])
    {
        if (!$tenantId || !$clientId || !$clientSecret || !$assertion) {
            throw new \InvalidArgumentException("Params cannot be empty");
        }
        $this->tenantId = $tenantId;
        $this->clientId = $clientId;
        $this->clientSecret = $clientSecret;
        $this->assertion = $assertion;
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
            'assertion' => $this->assertion,
            'grant_type' => $this->getGrantType(),
            'requested_token_use' => 'on_behalf_of'
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
        return 'urn:ietf:params:oauth:grant-type:jwt-bearer';
    }

    /**
     * @inheritDoc
     */
    public function getTenantId(): string
    {
        return $this->tenantId;
    }
}
