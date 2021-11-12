<?php
namespace Microsoft\Kiota\Abstractions;

use Http\Promise\Promise;

interface ResponseHandler {
    /**
     * Callback method that is invoked when a response is received.
     * @param mixed $response The native response object.
     * @return Promise A Promise that represents the asynchronous operation and contains the deserialized response.
     */
    public function handleResponseAsync($response): Promise;
}
