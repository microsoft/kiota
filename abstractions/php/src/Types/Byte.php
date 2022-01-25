<?php

namespace Microsoft\Kiota\Abstractions\Types;

use InvalidArgumentException;

/**
 * This class is a wrapper around unsigned int values upto 255.
 */
class Byte
{
    /**
     * The byte value
     * @var int $value
     */
    private int $value;

    /**
     * @param int $value The byte value
     */
    public function __construct(int $value) {
        if($value < 0 || $value > 255) {
            throw new InvalidArgumentException("Byte should be a value between 0-255 inclusive {$value} given");
        }
        $this->value = $value;
    }

    public function __toString(): string
    {
        return (string)$this->value;
    }
}