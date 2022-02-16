<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication\Oauth;


use Firebase\JWT\JWT;
use GuzzleHttp\Psr7\Utils;
use Ramsey\Uuid\Uuid;

class BaseCertificateContext implements TokenRequestContext
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
     * @var mixed Byte stream of the certificate
     */
    private $privateKey;

    /**
     * @param string $tenantId
     * @param string $clientId
     * @param string|null $certificatePath
     * @param string|null $certificatePassphrase
     */
    public function __construct(string $tenantId, string $clientId, string $certificatePath, string $certificatePassphrase = '')
    {
        if (!$tenantId || !$clientId || !$certificatePath) {
            throw new \InvalidArgumentException("TenantId, clientId and certificatePath cannot be empty");
        }
        $this->tenantId = $tenantId;
        $this->clientId = $clientId;
        $privateKey = openssl_pkey_get_private(file_get_contents($certificatePath), $certificatePassphrase);
        if (!$privateKey) {
            throw new \RuntimeException("Unable to read certificate at {$certificatePath} using passphrase {$certificatePassphrase}");
        } elseif (is_resource($privateKey)) {
            // PHP 7 openssl_pkey_get_private returns resource
            $this->privateKey = Utils::streamFor($privateKey)->getContents();
        } else {
            // PHP 8 openssl_pkey_get_private returns OpenSSLAsymmetricKey
            $this->privateKey = $privateKey;
        }
    }

    /**
     * @inheritDoc
     */
    public function getParams(): array
    {
        return [
            'client_id' => $this->clientId,
            'client_assertion' => $this->getClientAssertion(),
            'client_assertion_type' => 'urn:ietf:params:Oauth:client-assertion-type:jwt-bearer'
        ];
    }

    /**
     * @inheritDoc
     */
    public function getRefreshTokenParams(string $refreshToken): array
    {
        return [
            'client_id' => $this->clientId,
            'client_assertion' => $this->getClientAssertion(),
            'client_assertion_type' => 'urn:ietf:params:Oauth:client-assertion-type:jwt-bearer',
            'refresh_token' => $refreshToken,
            'grant_type' => 'refresh_token'
        ];
    }

    /**
     * @inheritDoc
     */
    public function getGrantType(): string
    {
        return '';
    }

    /**
     * @inheritDoc
     */
    public function getTenantId(): string
    {
        return $this->tenantId;
    }

    /**
     * Generates JSON Web Token ref (https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-certificate-credentials)
     * @return string
     */
    private function getClientAssertion(): string
    {
        $currentTimeSecs = time();
        $claims = [
            'aud' => "https://login.microsoftonline.com/{$this->tenantId}/v2.0",
            'iss' => $this->clientId,
            'jti' => Uuid::uuid4(), // random UUID based on RFC 4122
            'sub' => $this->clientId,
            'iat' => $currentTimeSecs,
            'nbf' => $currentTimeSecs,
            'exp' => $currentTimeSecs + (5 * 60), // add 5 minutes to iat
        ];
        return JWT::encode($claims, $this->privateKey, 'RS256');
    }
}
