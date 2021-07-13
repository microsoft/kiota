<?php


namespace Microsoft\Kiota\Abstractions\Serialization;


interface ParseNode {
    public function getChildNode(string $identifier): ParseNode;
    public function getStringValue(): string;
    public function getBooleanValue(): bool;
    public function getIntegerValue(): int;
    public function getFloatValue(): float;
    public function getUUIDValue(): string;
    public function getOffsetDateTimeValue(): \DateTimeInterface;

}
