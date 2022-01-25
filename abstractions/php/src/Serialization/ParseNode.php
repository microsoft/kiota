<?php


namespace Microsoft\Kiota\Abstractions\Serialization;

use DateTime;
use DateTimeInterface;
use Microsoft\Kiota\Abstractions\Enum;

interface ParseNode {
    /**
     * Gets a new parse node for the given identifier.
     * @param string $identifier the identifier of the current node property.
     * @return self a new parse node for the given identifier.
     */
    public function getChildNode(string $identifier): ParseNode;

    /**
     * Gets the string value of the node.
     * @return string the string value of the node.
     */
    public function getStringValue(): string;

    /**
     * Gets the boolean value of the node.
     * @return bool the boolean value of the node.
     */
    public function getBooleanValue(): bool;

    /**
     * Gets the Integer value of the node.
     * @return int the Integer value of the node.
     */
    public function getIntegerValue(): int;

    /**
     * Gets the Float value of the node.
     * @return float the Float value of the node.
     */
    public function getFloatValue(): float;

    /**
     * Gets the Long value of the node.
     * @return int the Long value of the node.
     */
    public function getLongValue(): int;

    /**
     * Gets the UUID value of the node.
     * @return string the UUID value of the node.
     */
    public function getUUIDValue(): string;

    /**
     * Gets the model object value of the node.
     * @param string $type The type for the Parsable object.
     * @return Parsable the model object value of the node.
     */
    public function getObjectValue(string $type): Parsable;

    /**
     * @param string $type The underlying type for the Parsable class.
     * @return array<Parsable> An array of Parsable values.
     */
    public function getCollectionOfObjectValues(string $type): array;

    /**
     * Get a collection of values that are not parsable in Nature.
     * @return array<mixed> A collection of primitive values.
     */
    public function getCollectionOfPrimitiveValues(): array;

    /**
     * Gets the OffsetDateTime value of the node.
     * @return DateTime the OffsetDateTime value of the node.
     */
    public function getDateTimeOffsetValue(): DateTime;

    /**
     * Gets the Enum value of the node.
     * @param string $targetEnum
     * @return Enum the Enum value of the node.
     */
    public function getEnumValue(string $targetEnum): Enum;

    /**
     * Gets the EnumSet value of the node.
     * @param Enum $targetEnum
     * @return array<string> the EnumSet value of the node.
     */
    public function getEnumSetValue(Enum $targetEnum): array;

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
