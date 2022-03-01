<?php

namespace Microsoft\Kiota\Abstractions\Authentication;

use Http\Promise\FulfilledPromise;
use Http\Promise\Promise;
use Microsoft\Kiota\Abstractions\RequestInformation;

/**
 * Class BaseBearerTokenAuthenticationProvider
 *
 * Provides a base class for implementing {@link AuthenticationProvider} for Bearer authentication scheme
 *
 * @package Microsoft\Kiota\Abstractions\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class BaseBearerTokenAuthenticationProvider implements AuthenticationProvider {

    /**
     * @var string $authorizationHeaderKey The Authorization header key
     */
    private static string $authorizationHeaderKey = "Authorization";

    /**
     * @var AccessTokenProvider {@link AccessTokenProvider}
     */
    private AccessTokenProvider $accessTokenProvider;

    /**
     * Creates a new instance
     * @param AccessTokenProvider $accessTokenProvider to use for getting the access token
     */
    public function __construct(AccessTokenProvider $accessTokenProvider)
    {
        $this->accessTokenProvider = $accessTokenProvider;
    }

    /**
     * Gets the {@link AccessTokenProvider} used for getting the access token
     *
     * @return AccessTokenProvider
     */
    public function getAccessTokenProvider(): AccessTokenProvider
    {
        return $this->accessTokenProvider;
    }

    /**
     * @param RequestInformation $request
     * @return Promise
     */
    public function authenticateRequest(RequestInformation $request): Promise {
        if (!array_key_exists(self::$authorizationHeaderKey, $request->headers)) {
            return $this->accessTokenProvider->getAuthorizationTokenAsync($request->getUri())
                        ->then(function ($token) use($request) {
                            if ($token) {
                                $request->headers[self::$authorizationHeaderKey] = "Bearer {$token}";
                            }
                            return null;
                        });
        }
        return new FulfilledPromise(null);
    }
}
