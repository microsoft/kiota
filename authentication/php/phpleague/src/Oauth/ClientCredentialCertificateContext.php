<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication\Oauth;

/**
 * Class ClientCredentialCertificateContext
 *
 * client_credentials flow using certificate
 *
 * @package Microsoft\Kiota\Authentication\Oauth
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class ClientCredentialCertificateContext extends BaseCertificateContext
{
    private array $additionalParams;

    /**
     * @param string $tenantId
     * @param string $clientId
     * @param string $certificatePath
     * @param string $certificatePassphrase
     * @param array $additionalParams
     */
    public function __construct(string $tenantId, string $clientId, string $certificatePath, string $certificatePassphrase = '', array $additionalParams = [])
    {
        $this->additionalParams = $additionalParams;
        parent::__construct($tenantId, $clientId, $certificatePath, $certificatePassphrase);
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
