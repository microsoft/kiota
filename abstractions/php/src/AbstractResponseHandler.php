<?php
namespace Microsoft\Kiota\Abstractions;

use Http\Promise\Promise;

abstract class AbstractResponseHandler {
    abstract public function handleResponseAsync(): Promise;
}
