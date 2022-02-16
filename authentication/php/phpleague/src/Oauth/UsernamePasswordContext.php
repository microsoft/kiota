<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication\Oauth;

/**
 * Class UsernamePasswordContext
 *
 * Params for the Resource Owner Password Credentials flow using a secret
 *
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class UsernamePasswordContext extends BaseSecretContext
{
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
        if (!$username || !$password) {
            throw new \InvalidArgumentException("Username and password cannot be empty");
        }
        $this->username = $username;
        $this->password = $password;
        $this->additionalParams = $additionalParams;
        parent::__construct($tenantId, $clientId, $clientSecret);
    }

    /**
     * @inheritDoc
     */
    public function getParams(): array
    {
        return array_merge($this->additionalParams, parent::getParams(), [
            'username' => $this->username,
            'password' => $this->password,
            'grant_type' => $this->getGrantType()
        ]);
    }

    /**
     * @inheritDoc
     */
    public function getGrantType(): string
    {
        return 'password';
    }
}
