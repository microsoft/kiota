<?php
namespace Microsoft\Kiota\Abstractions\Authentication;

use Http\Promise\Promise;
use Microsoft\Kiota\Abstractions\RequestInformation;

interface AuthenticationProvider {
    /**
     * @param RequestInformation $request
     * @return Promise
     */
    public function authenticateRequest(RequestInformation $request): Promise;
}
