<?php
namespace Microsoft\Kiota\Abstractions;

use Http\Promise\Promise;

interface HttpCoreInterface {
    public function sendAsync(RequestInfo $requestInfo, object $targetClass, AbstractResponseHandler $responseHandler): Promise;
}
