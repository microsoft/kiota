<?php
namespace Microsoft\Kiota\Abstractions;

use Http\Promise\Promise;

interface AuthenticationProvider {
    public function getAuthorizationToken(string $requestUrl): Promise;
}
