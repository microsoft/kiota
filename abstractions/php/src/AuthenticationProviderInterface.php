<?php
namespace Microsoft\Kiota\Abstractions;

use Http\Promise\Promise;

interface AuthenticationProviderInterface {
    public function getAuthorizationToken(string $requestUrl): Promise;
}
