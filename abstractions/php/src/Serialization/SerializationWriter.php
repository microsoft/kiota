<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

use Psr\Http\Message\StreamInterface;

interface SerializationWriter {
    public function writeStringValue(string $key, string $value): void;
    public function writeBooleanValue(string $key, bool $value): void;
    public function writeDecimalValue(string $key, float $value): void;
    public function writeNumberValue(string $key, int $value): void;
    public function writeUUIDValue(string $key, string $value): void;
    public function writeOffsetDateTimeValue(string $key, \DateInterval $value): void;
    public function writeCollectionOfPrimitiveValues(string $key, array $values): void;
    public function writeCollectionOfObjectValues(string $key, array $values): void;
    public function writeObjectValue(string $key,object $value): void;
    public function getSerializedContent(): StreamInterface;
    public function writeEnumSetValue(string $key, array $values): void;
    public function writeEnumValue(string $key, object $value): void;
    public function writeAdditionalData(array $value): void;
    // These use the consumer interface
    public function getOnBeforeObjectSerialization(): Parsable;
    public function getOnAfterObjectSerialization(): Parsable;
    public function setOnBeforeObjectSerialization(callable $value): ?callable;
    public function setOnAfterObjectSerialization(callable $value): void;
}
