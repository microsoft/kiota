<?php

namespace Microsoft\Kiota\Abstractions;
use Exception;

class ApiException extends Exception
{
    /**
     * @param string $message
     * @param int $code
     * @param Exception|null $innerException
     */
    public function __construct(string $message = "", int $code = 0, ?Exception $innerException = null) {
        parent::__construct($message, $code, $innerException);
    }
}