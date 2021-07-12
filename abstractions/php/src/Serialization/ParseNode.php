<?php


namespace Microsoft\Kiota\Abstractions\Serialization;

use DateTimeInterface;
use Closure;

abstract class ParseNode {
    abstract public function getChildNode(string $identifier): ParseNode;
    abstract public function getStringValue(): string;
    abstract public function getBooleanValue(): bool;
    abstract public function getIntegerValue(): int;
    abstract public function getFloatValue(): float;
    abstract public function getUUIDValue(): string;
    abstract public function getOffsetDateTimeValue(): DateTimeInterface;
    public ?Closure $onBeforeAssignFieldValues;
    public ?Closure $onAfterAssignFieldValues;
}
