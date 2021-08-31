<?php
namespace Microsoft\Kiota\Abstractions;

use Http\Promise\Promise;

abstract class ResponseHandler {
    abstract public function handleResponseAsync(): Promise;
}
