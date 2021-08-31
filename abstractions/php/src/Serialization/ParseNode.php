<?php


namespace Microsoft\Kiota\Abstractions\Serialization;

use DateTimeInterface;
use Closure;

abstract class ParseNode {
    /**
     * @param string $identifier
     * @return ParseNode
     */
    abstract public function getChildNode(string $identifier): ParseNode;

    /**
     * @return string
     */
    abstract public function getStringValue(): string;

    /**
     * @return bool
     */
    abstract public function getBooleanValue(): bool;

    /**
     * @return int
     */
    abstract public function getIntegerValue(): int;

    /**
     * @return float
     */
    abstract public function getFloatValue(): float;

    /**
     * @return string
     */
    abstract public function getUUIDValue(): string;

    /**
     * @return DateTimeInterface
     */
    abstract public function getOffsetDateTimeValue(): DateTimeInterface;

    /**
     * @var Closure|null
     */
    public ?Closure $onBeforeAssignFieldValues;

    /**
     * @var Closure|null
     */
    public ?Closure $onAfterAssignFieldValues;
}
