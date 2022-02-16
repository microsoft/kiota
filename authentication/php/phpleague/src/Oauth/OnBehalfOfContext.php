<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication\Oauth;

/**
 * Class OnBehalfOfContext
 *
 * Token request parameters for the on_behalf_of flow using a secret
 *
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class OnBehalfOfContext extends BaseSecretContext
{
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
        if (!$assertion) {
            throw new \InvalidArgumentException("Assertion cannot be empty");
        }
        $this->assertion = $assertion;
        $this->additionalParams = $additionalParams;
        parent::__construct($tenantId, $clientId, $clientSecret);
    }

    /**
     * @inheritDoc
     */
    public function getParams(): array
    {
        return array_merge($this->additionalParams, parent::getParams(), [
            'assertion' => $this->assertion,
            'grant_type' => $this->getGrantType(),
            'requested_token_use' => 'on_behalf_of'
        ]);
    }

    /**
     * @inheritDoc
     */
    public function getGrantType(): string
    {
        return 'urn:ietf:params:Oauth:grant-type:jwt-bearer';
    }
}
