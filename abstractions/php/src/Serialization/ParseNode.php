<?php

namespace Microsoft\Kiota\Abstractions\Serialization;

use DateInterval;
use DateTime;
use Microsoft\Kiota\Abstractions\Enum;
use Microsoft\Kiota\Abstractions\Types\Byte;
use Microsoft\Kiota\Abstractions\Types\Date;
use Microsoft\Kiota\Abstractions\Types\Time;
use Psr\Http\Message\StreamInterface;

interface ParseNode {
    /**
     * Gets a new parse node for the given identifier.
     * @param string $identifier the identifier of the current node property.
     * @return self|null a new parse node for the given identifier.
     */
    public function getChildNode(string $identifier): ?ParseNode;

    /**
     * Gets the string value of the node.
     * @return string|null the string value of the node.
     */
    public function getStringValue(): ?string;

    /**
     * Gets the boolean value of the node.
     * @return bool|null the boolean value of the node.
     */
    public function getBooleanValue(): ?bool;

    /**
     * Gets the Integer value of the node.
     * @return int|null the Integer value of the node.
     */
    public function getIntegerValue(): ?int;

    /**
     * Gets the Float value of the node.
     * @return float|null the Float value of the node.
     */
    public function getFloatValue(): ?float;

    /**
     * Gets the model object value of the node.
     * @param string $type The type for the Parsable object.
     * @return Parsable|null the model object value of the node.
     */
    public function getObjectValue(string $type): ?Parsable;

    /**
     * @param string $type The underlying type for the Parsable class.
     * @return array<Parsable>|null An array of Parsable values.
     */
    public function getCollectionOfObjectValues(string $type): ?array;

    /**
     * Get a collection of values that are not parsable in Nature.
     * @param string|null $typeName
     * @return array<mixed>|null A collection of primitive values.
     */
    public function getCollectionOfPrimitiveValues(?string $typeName = null): ?array;

    /**
     * Gets the DateTimeValue of the node
     * @return DateTime|null
     */
    public function getDateTimeValue(): ?DateTime;

    /**
     * Gets the DateInterval value of the node
     * @return DateInterval|null
     */
    public function getDateIntervalValue(): ?DateInterval;

    /**
     * Gets the Date only value of the node
     * @return Date|null
     */
    public function getDateValue(): ?Date;

    /**
     * Gets the Time only value of the node
     * @return Time|null
     */
    public function getTimeValue(): ?Time;

    /**
     * Gets the Enum value of the node.
     * @param string $targetEnum
     * @return Enum|null the Enum value of the node.
     */
    public function getEnumValue(string $targetEnum): ?Enum;

    /**
     * Return a byte value.
     * @return Byte|null
     */
    public function getByteValue(): ?Byte;

    /**
     * Get a Stream from node.
     * @return StreamInterface|null
     */
    public function getBinaryContent(): ?StreamInterface;
    /**
     * Gets the callback called before the node is deserialized.
     * @return callable the callback called before the node is deserialized.
     */
    public function getOnBeforeAssignFieldValues(): ?callable;

    /**
     * Gets the callback called after the node is deserialized.
     * @return callable the callback called after the node is deserialized.
     */
    public function getOnAfterAssignFieldValues(): ?callable;

    /**
     * Sets the callback called after the node is deserialized.
     * @param callable $value the callback called after the node is deserialized.
     */
    public function setOnAfterAssignFieldValues(callable $value): void;

    /**
     * Sets the callback called before the node is deserialized.
     * @param callable $value the callback called before the node is deserialized.
     */
    public function setOnBeforeAssignFieldValues(callable $value): void;
}
