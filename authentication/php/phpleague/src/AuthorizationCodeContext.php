<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication;

/**
 * Class AuthorizationCodeContext
 *
 * Request params for the token request of the authorization_code flow using a secret
 *
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class AuthorizationCodeContext extends BaseSecretContext
{
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
        if (!$authCode || !$redirectUri) {
            throw new \InvalidArgumentException("Auth code and redirectUri cannot be empty");
        }
        $this->authCode = $authCode;
        $this->redirectUri = $redirectUri;
        $this->additionalParams = $additionalParams;
        parent::__construct($tenantId, $clientId, $clientSecret);
    }

    /**
     * @inheritDoc
     */
    public function getParams(): array
    {
        return array_merge($this->additionalParams, parent::getParams(), [
            'code' => $this->authCode,
            'redirect_uri' => $this->redirectUri,
            'grant_type' => $this->getGrantType()
        ]);
    }

    /**
     * @inheritDoc
     */
    public function getGrantType(): string
    {
        return 'authorization_code';
    }
}
