<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Authentication;


use Http\Promise\FulfilledPromise;
use Http\Promise\Promise;
use Http\Promise\RejectedPromise;
use League\OAuth2\Client\Grant\GrantFactory;
use League\OAuth2\Client\Provider\GenericProvider;
use League\OAuth2\Client\Token\AccessToken;
use Microsoft\Kiota\Abstractions\Authentication\AccessTokenProvider;
use Microsoft\Kiota\Abstractions\Authentication\AllowedHostsValidator;

/**
 * Class PhpLeagueAccessTokenProvider
 * @package Microsoft\Kiota\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class PhpLeagueAccessTokenProvider implements AccessTokenProvider
{
    /**
     * @var TokenRequestContext {@link TokenRequestContext}
     */
    private TokenRequestContext $tokenRequestContext;
    /**
     * @var AllowedHostsValidator Validates whether a token should be fetched for a request url
     */
    private AllowedHostsValidator $allowedHostsValidator;
    /**
     * @var array<string, string>
     */
    private array $scopes;
    /**
     * @var AccessToken|null Token object to re-use before expiry
     */
    private ?AccessToken $cachedToken = null;
    /**
     * @var GenericProvider OAuth 2.0 provider from PHP League library
     */
    private GenericProvider $oauthProvider;

    /**
     * Creates a new instance
     * @param TokenRequestContext $tokenRequestContext
     * @param array $scopes
     * @param array $allowedHosts
     */
    public function __construct(TokenRequestContext $tokenRequestContext, array $scopes, array $allowedHosts = [])
    {

        $this->tokenRequestContext = $tokenRequestContext;
        if (empty($scopes)) {
            throw new \InvalidArgumentException("Scopes cannot be empty");
        }
        $this->scopes = $scopes;
        $this->allowedHostsValidator = new AllowedHostsValidator($allowedHosts);
        $this->initOauthProvider();
    }

    /**
     * @inheritDoc
     */
    public function getAuthorizationTokenAsync(string $url): Promise
    {
        if (!$this->getAllowedHostsValidator()->isUrlHostValid($url)) {
            return new FulfilledPromise(null);
        }
        try {
            if ($this->cachedToken && $this->cachedToken->getExpires() && $this->cachedToken->hasExpired() && $this->cachedToken->getRefreshToken()) {
                $this->cachedToken = $this->oauthProvider->getAccessToken('refresh_token',
                    array_merge($this->tokenRequestContext->getParams(), ['refresh_token' => $this->cachedToken->getRefreshToken()])
                );
            } else {
                $params = array_merge($this->tokenRequestContext->getParams(), ['scope' => implode(',', $this->scopes)]);
                $this->cachedToken = $this->oauthProvider->getAccessToken($this->tokenRequestContext->getGrantType(), $params);
            }
            return new FulfilledPromise($this->cachedToken->getToken());
        } catch (\Exception $ex) {
            return new RejectedPromise($ex);
        }
    }

    /**
     * @inheritDoc
     */
    public function getAllowedHostsValidator(): AllowedHostsValidator
    {
        return $this->allowedHostsValidator;
    }

    /**
     * Returns the underlying OAuth provider
     *
     * @return GenericProvider
     */
    public function getOauthProvider(): GenericProvider
    {
        return $this->oauthProvider;
    }

    /**
     * Initialises a PHP League provider
     */
    private function initOauthProvider(): void
    {
        $grantFactory = new GrantFactory();
        // Add our custom grant type to the registry
        $grantFactory->setGrant('urn:ietf:params:oauth:grant-type:jwt-bearer', new OnBehalfOfGrant());

        $this->oauthProvider = new GenericProvider([
            'urlAccessToken' => "https://login.microsoftonline.com/{$this->tokenRequestContext->getTenantId()}/oauth2/v2.0/token",
            'urlAuthorize' => '', // required to instantiate Generic Provider
            'urlResourceOwnerDetails' => '' // required to instantiate Generic Provider
        ], [
            'grantFactory' => $grantFactory
        ]);
    }

}
