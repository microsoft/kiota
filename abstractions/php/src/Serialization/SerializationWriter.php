<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

use DateInterval;
use DateTime;
use Microsoft\Kiota\Abstractions\Enum;
use Microsoft\Kiota\Abstractions\Types\Byte;
use Microsoft\Kiota\Abstractions\Types\Date;
use Microsoft\Kiota\Abstractions\Types\Time;
use Psr\Http\Message\StreamInterface;

/** Defines an interface for serialization of objects to a stream. */
interface SerializationWriter {
    /**
     * Writes the specified string value to the stream with an optional given key.
     * @param string|null $key the key to write the value with.
     * @param string|null $value the value to write to the stream.
     */
    public function writeStringValue(?string $key, ?string $value): void;

    /**
     * Writes the specified Boolean value to the stream with an optional given key.
     * @param string|null $key the key to write the value with.
     * @param bool|null $value the value to write to the stream.
     */
    public function writeBooleanValue(?string $key, ?bool $value): void;

    /**
     * Writes the specified Float value to the stream with an optional given key.
     * @param string|null $key the key to write the value with.
     * @param float|null $value the value to write to the stream.
     */
    public function writeFloatValue(?string $key, ?float $value): void;

    /**
     * Writes the specified Integer value to the stream with an optional given key.
     * @param string|null $key the key to write the value with.
     * @param int $value the value to write to the stream.
     */
    public function writeIntegerValue(?string $key, ?int $value): void;

    /**
     * Writes the specified Long value to the stream with an optional given key.
     * @param string|null $key the key to write the value with.
     * @param int|null $value the value to write to the stream.
     */
    public function writeLongValue(?string $key, ?int $value): void;


    /**
     * Writes the specified UUID value to the stream with an optional given key.
     * @param string|null $key the key to write the value with.
     * @param string|null $value the value to write to the stream.
     */
    public function writeUUIDValue(?string $key, ?string $value): void;

    /**
     * Writes the specified OffsetDateTime value to the stream with an optional given key.
     * @param string|null $key the key to write the value with.
     * @param DateTime|null $value the value to write to the stream.
     */
    public function writeDateTimeOffsetValue(?string $key, ?DateTime $value): void;

    /**
     * Writes the specified collection of object values to the stream with an optional given key.
     * @param string|null $key the key to write the value with.
     * @param array<Parsable>|null $values
     */
    public function writeCollectionOfObjectValues(?string $key, ?array $values): void;

    /**
     * Writes the specified model object value to the stream with an optional given key.
     * @param string|null $key the key to write the value with.
     * @param Parsable|null $value the value to write to the stream.
     */
    public function writeObjectValue(?string $key, ?Parsable $value): void;

    /**
     * Gets the value of the serialized content.
     * @return StreamInterface the value of the serialized content.
     */
    public function getSerializedContent(): StreamInterface;

    /**
     * Writes the specified enum set value to the stream with an optional given key.
     * @param string|null $key the key to write the value with.
     * @param array<Enum>|null $values the value to write to the stream.
     */
    public function writeEnumSetValue(?string $key, ?array $values): void;

    /**
     * Writes the specified enum value to the stream with an optional given key.
     * @param string|null $key the key to write the value with.
     * @param Enum|null $value the value to write to the stream.
     */
    public function writeEnumValue(?string $key, ?Enum $value): void;

    /**
     * Writes a null value for the specified key.
     * @param string|null $key the key to write the value with.
     */
    public function writeNullValue(?string $key): void;

    /**
     * Writes the specified additional data values to the stream with an optional given key.
     * @param array<string,mixed> $value the values to write to the stream.
     */
    public function writeAdditionalData(?array $value): void;

    /**
     * Write the Date-only Segment of DateTime.
     * @param string|null $key
     * @param Date|null $value
     */
    public function writeDateOnlyValue(?string $key, ?Date $value): void;

    /**
     * Write a TimeOnly value without the
     * @param string|null $key
     * @param Time|null $value
     * @return void
     */
    public function writeTimeOnlyValue(?string $key, ?Time $value): void;

    /**
     * Write a byte value.
     * @param string|null $key
     * @param Byte|null $value
     * @return void
     */
    public function writeByteValue(?string $key, ?Byte $value): void;

    /**
     * Sets the callback called before the objects gets serialized.
     * @param callable|null $value the callback called before the objects gets serialized.
     */
    public function setOnBeforeObjectSerialization(?callable $value): void;

    /**
     * Gets the callback called before the object gets serialized.
     * @return callable|null the callback called before the object gets serialized.
     */
    public function getOnBeforeObjectSerialization(): ?callable;

    /**
     * Sets the callback called after the objects gets serialized.
     * @param callable|null $value the callback called after the objects gets serialized.
     */
    public function setOnAfterObjectSerialization(?callable $value): void;

    /**
     * Gets the callback called after the object gets serialized.
     * @return callable|null the callback called after the object gets serialized.
     */
    public function getOnAfterObjectSerialization(): ?callable;

    /**
     * Sets the callback called right after the serialization process starts.
     * @param callable|null $value the callback called right after the serialization process starts.
     */
    public function setOnStartObjectSerialization(?callable $value): void;

    /**
     * Gets the callback called right after the serialization process starts.
     * @return callable|null the callback called right after the serialization process starts.
     */
    public function getOnStartObjectSerialization(): ?callable;
}
