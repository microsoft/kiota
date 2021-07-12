<?php
namespace Microsoft\Kiota\Abstractions;

use Http\Promise\Promise;

interface HttpCore {
    public function sendAsync(RequestInfo $requestInfo, object $targetClass, ResponseHandler $responseHandler): Promise;
}
