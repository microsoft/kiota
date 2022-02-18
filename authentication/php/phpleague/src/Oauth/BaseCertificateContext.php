<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication\Oauth;


use Firebase\JWT\JWT;
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

    private string $certificateFingerprint;
    /**
     * @var string JWT token signed with the private key
     */
    private string $clientAssertion;

    /**
     * @param string $tenantId
     * @param string $clientId
     * @param string $certificatePath PEM file containing the certificate
     * @param string $privateKeyPath PEM file containing the certificate's private key
     * @param string $privateKeyPassphrase password protecting the private key
     */
    public function __construct(string $tenantId,
                                string $clientId,
                                string $certificatePath,
                                string $privateKeyPath,
                                string $privateKeyPassphrase = '')
    {
        if (!$tenantId || !$clientId || !$certificatePath || !$privateKeyPath) {
            throw new \InvalidArgumentException("TenantId, clientId, certificatePath an privateKeyPath cannot be empty");
        }
        $this->tenantId = $tenantId;
        $this->clientId = $clientId;
        $certificate = openssl_x509_read(file_get_contents($certificatePath));
        if (!$certificate) {
            throw new \InvalidArgumentException("Could not read X.509 certificate at {$certificatePath}");
        }
        $this->certificateFingerprint = openssl_x509_fingerprint($certificate);
        $this->privateKey = openssl_pkey_get_private(file_get_contents($privateKeyPath), $privateKeyPassphrase);
        if (!$this->privateKey) {
            throw new \InvalidArgumentException("Unable to read private key at {$privateKeyPath} using passphrase {$privateKeyPassphrase}");
        }
        if (!openssl_x509_check_private_key($certificate, $this->privateKey)) {
            throw new \InvalidArgumentException("Private Key at {$privateKeyPath} does not correspond to the certificate at {$certificatePath}");
        }
        $this->clientAssertion = $this->getClientAssertion();
    }

    /**
     * @inheritDoc
     */
    public function getParams(): array
    {
        return [
            'client_id' => $this->clientId,
            'client_assertion' => $this->clientAssertion,
            'client_assertion_type' => 'urn:ietf:params:oauth:client-assertion-type:jwt-bearer'
        ];
    }

    /**
     * @inheritDoc
     */
    public function getRefreshTokenParams(string $refreshToken): array
    {
        return [
            'client_id' => $this->clientId,
            'client_assertion' => $this->clientAssertion,
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
        return JWT::encode($claims, $this->privateKey, 'RS256', null, [
            'x5t' => JWT::urlsafeB64Encode( hex2bin($this->certificateFingerprint))
        ]);
    }
}
