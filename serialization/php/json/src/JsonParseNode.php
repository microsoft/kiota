<?php

namespace Microsoft\Kiota\Serialization\Json;

use DateTimeInterface;
use InvalidArgumentException;
use Microsoft\Kiota\Abstractions\Enum;
use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use RuntimeException;

/**
 * @method onBeforeAssignFieldValues(Parsable $result)
 * @method onAfterAssignFieldValues(Parsable $result)
 */
class JsonParseNode implements ParseNode
{
    /** @var mixed|null $jsonNode*/
    private $jsonNode;

    /** @var callable|null */
    public $onBeforeAssignFieldValues;
    /** @var callable|null */
    public $onAfterAssignFieldValues;
    /**
     * @param mixed $content
     */
    public function __construct($content) {
        if ($content === null) {
            return;
        }
        $this->jsonNode = $content;

    }

    /**
     * @inheritDoc
     */
    public function getChildNode(string $identifier): ParseNode {
        return new self($this->jsonNode[$identifier] ?? null);
    }

    /**
     * @inheritDoc
     */
    public function getStringValue(): string {
        return $this->jsonNode;
    }

    /**
     * @inheritDoc
     */
    public function getBooleanValue(): bool {
        return (bool)$this->jsonNode;
    }

    /**
     * @inheritDoc
     */
    public function getIntegerValue(): int {
        return (int)$this->jsonNode;
    }

    /**
     * @inheritDoc
     */
    public function getFloatValue(): float {
        return (float)$this->jsonNode;
    }

    /**
     * @inheritDoc
     */
    public function getLongValue(): int {
        return $this->getIntegerValue();
    }

    /**
     * @inheritDoc
     */
    public function getUUIDValue(): string {
        return $this->getStringValue();
    }

    /**
     * @return array<Parsable>
     * @throws \Exception
     */
    public function getCollectionOfObjectValues(string $type): array {
        return array_map(static function ($val) use($type) {
            return $val->getObjectValue($type);
        }, array_map(static function ($value) {
            return new JsonParseNode($value);
        }, $this->jsonNode));
    }

    /**
     * @return array<mixed>
     */
    public function getCollectionOfPrimitiveObjectValues(): array {
        return [];
    }

    /**
     * @inheritDoc
     * @throws \Exception
     */
    public function getObjectValue(?string $type = null): Parsable {
        if ($type === null){
            throw new RuntimeException();
        }
        /** @var Parsable $result */
        $result = new ($type);
        if($this->onBeforeAssignFieldValues !== null) {
            $this->onBeforeAssignFieldValues($result);
        }
        $this->assignFieldValues($result);
        if ($this->onAfterAssignFieldValues !== null){
            $this->onAfterAssignFieldValues($result);
        }
        return $result;
    }

    /**
     * @param Parsable $result
     * @return void
     */
    private function assignFieldValues(Parsable $result): void {
        $fieldDeserializers = $result->getFieldDeserializers();

        foreach ($this->jsonNode as $key => $value){
            $deserializer = $fieldDeserializers[$key] ?? null;

            if ($deserializer !== null){
                $deserializer($result, new JsonParseNode($value));
            } else {
                $result->getAdditionalData()[$key] = $value;
            }
        }
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
    public function getEnumValue(string $targetEnum): Enum{
        if (!is_subclass_of($targetEnum, Enum::class)) {
            throw new InvalidArgumentException('Invalid enum provided.');
        }
        return new ($targetEnum)();
    }

    /**
     * @inheritDoc
     */
    public function getEnumSetValue(Enum $targetEnum): array {
        return [$targetEnum->value()];
    }

    /**
     * @inheritDoc
     */
    public function getOnBeforeAssignFieldValues(): ?callable {
        return $this->onBeforeAssignFieldValues;
    }

    /**
     * @inheritDoc
     */
    public function getOnAfterAssignFieldValues(): ?callable {
        return $this->onAfterAssignFieldValues;
    }

    /**
     * @inheritDoc
     */
    public function setOnAfterAssignFieldValues(callable $value): void {
        $this->onAfterAssignFieldValues = $value;
    }

    /**
     * @inheritDoc
     */
    public function setOnBeforeAssignFieldValues(callable $value): void {
        $this->onBeforeAssignFieldValues = $value;
    }
}