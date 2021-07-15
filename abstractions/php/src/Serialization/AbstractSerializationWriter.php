<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

use Psr\Http\Message\StreamInterface;

use Closure;
abstract class AbstractSerializationWriter {
    abstract public function writeStringValue(string $key, string $value): void;
    abstract public function writeBooleanValue(string $key, bool $value): void;
    abstract public function writeDecimalValue(string $key, float $value): void;
    abstract public function writeNumberValue(string $key, int $value): void;
    abstract public function writeUUIDValue(string $key, string $value): void;
    abstract public function writeOffsetDateTimeValue(string $key, \DateInterval $value): void;
    abstract public function writeCollectionOfPrimitiveValues(string $key, array $values): void;
    abstract public function writeCollectionOfObjectValues(string $key, array $values): void;
    abstract public function writeObjectValue(string $key,object $value): void;
    abstract public function getSerializedContent(): StreamInterface;
    abstract public function writeEnumSetValue(string $key, array $values): void;
    abstract public function writeEnumValue(string $key, object $value): void;
    abstract public function writeAdditionalData(array $value): void;
    public Closure $onBeforeObjectSerialization;
    public Closure $onAfterObjectSerialization;
}
