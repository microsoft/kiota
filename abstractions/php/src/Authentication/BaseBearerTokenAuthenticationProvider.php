<?php

namespace Microsoft\Kiota\Abstractions\Authentication;

use Http\Promise\FulfilledPromise;
use Http\Promise\Promise;
use Microsoft\Kiota\Abstractions\RequestInformation;

abstract class BaseBearerTokenAuthenticationProvider implements AuthenticationProvider {

    /**
     * @var string $authorizationHeaderKey The Authorization header key
     */
    private static string $authorizationHeaderKey = "Authorization";
    /**
     * @param RequestInformation $request
     * @return Promise
     */
    public function authenticateRequest(RequestInformation $request): Promise {
        if (!array_key_exists(self::$authorizationHeaderKey, $request->headers)) {
            return $this->getAuthorizationToken($request)
                        ->then(function ($token) use($request) {
                            if(is_null($token)){
                                throw new \DomainException('Could not get an authorization token');
                            }
                            $request->headers[self::$authorizationHeaderKey] = "Bearer {$token}";
                            return null;
                        });
        }
        return new FulfilledPromise(null);
    }

    /**
     * @param RequestInformation $request
     * @return Promise
     */
    abstract public function getAuthorizationToken(RequestInformation $request): Promise;
}
