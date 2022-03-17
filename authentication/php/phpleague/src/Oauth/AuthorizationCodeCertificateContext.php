<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication\Oauth;

/**
 * Class AuthorizationCodeCertificateContext
 *
 * authorization_code flow token request step using certificate
 *
 * @package Microsoft\Kiota\Authentication\Oauth
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class AuthorizationCodeCertificateContext extends BaseCertificateContext
{
    private string $authCode;
    private string $redirectUri;
    private array $additionalParams;

    /**
     * @param string $tenantId
     * @param string $clientId
     * @param string $authCode
     * @param string $redirectUri
     * @param string $certificatePath
     * @param string $privateKeyPath
     * @param string $privateKeyPassphrase
     * @param array $additionalParams
     */
    public function __construct(string $tenantId, string $clientId, string $authCode, string $redirectUri, string $certificatePath, string $privateKeyPath, string $privateKeyPassphrase = '', array $additionalParams = [])
    {
        if (!$authCode || !$redirectUri) {
            throw new \InvalidArgumentException("Auth code and redirectUri cannot be empty");
        }
        $this->authCode = $authCode;
        $this->redirectUri = $redirectUri;
        $this->additionalParams = $additionalParams;
        parent::__construct($tenantId, $clientId, $certificatePath, $privateKeyPath, $privateKeyPassphrase);
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
