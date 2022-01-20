<?php

namespace Microsoft\Kiota\Serialization\Json;

use DateTimeInterface;
use Microsoft\Kiota\Abstractions\Enum;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;

class JsonParseNode implements ParseNode
{
    /** @var mixed|null $jsonNode*/
    private $jsonNode;

    /**
     * @param mixed$content
     */
    public function __construct($content) {
        $this->jsonNode = $content;

    }

    /**
     * @inheritDoc
     */
    public function getChildNode(string $identifier): ParseNode {
        // TODO: Implement getChildNode() method.
    }

    /**
     * @inheritDoc
     */
    public function getStringValue(): string {
        // TODO: Implement getStringValue() method.
    }

    /**
     * @inheritDoc
     */
    public function getBooleanValue(): bool {
        // TODO: Implement getBooleanValue() method.
    }

    /**
     * @inheritDoc
     */
    public function getIntegerValue(): int {
        // TODO: Implement getIntegerValue() method.
    }

    /**
     * @inheritDoc
     */
    public function getFloatValue(): float {
        // TODO: Implement getFloatValue() method.
    }

    /**
     * @inheritDoc
     */
    public function getLongValue(): int {
        // TODO: Implement getLongValue() method.
    }

    /**
     * @inheritDoc
     */
    public function getUUIDValue(): string {
        // TODO: Implement getUUIDValue() method.
    }

    /**
     * @inheritDoc
     */
    public function getObjectValue(): object {
        // TODO: Implement getObjectValue() method.
    }

    /**
     * @inheritDoc
     */
    public function getDateTimeOffsetValue(): DateTimeInterface {
        // TODO: Implement getDateTimeOffsetValue() method.
    }

    /**
     * @inheritDoc
     */
    public function getEnumValue(string $targetEnum): Enum {
        // TODO: Implement getEnumValue() method.
    }

    /**
     * @inheritDoc
     */
    public function getEnumSetValue(Enum $targetEnum): array {
        // TODO: Implement getEnumSetValue() method.
    }

    /**
     * @inheritDoc
     */
    public function getOnBeforeAssignFieldValues(): ?callable {
        // TODO: Implement getOnBeforeAssignFieldValues() method.
    }

    /**
     * @inheritDoc
     */
    public function getOnAfterAssignFieldValues(): ?callable {
        // TODO: Implement getOnAfterAssignFieldValues() method.
    }

    /**
     * @inheritDoc
     */
    public function setOnAfterAssignFieldValues(callable $value): void {
        // TODO: Implement setOnAfterAssignFieldValues() method.
    }

    /**
     * @inheritDoc
     */
    public function setOnBeforeAssignFieldValues(callable $value): void {
        // TODO: Implement setOnBeforeAssignFieldValues() method.
    }
}