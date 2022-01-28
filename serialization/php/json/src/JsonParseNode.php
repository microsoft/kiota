<?php

namespace Microsoft\Kiota\Serialization\Json;

use DateInterval;
use DateTime;
use Exception;
use InvalidArgumentException;
use Microsoft\Kiota\Abstractions\Enum;
use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Types\Byte;
use Microsoft\Kiota\Abstractions\Types\Date;
use Microsoft\Kiota\Abstractions\Types\Time;
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
     * @param mixed|null $content
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
    public function getChildNode(string $identifier): ?ParseNode {
        if ($this->jsonNode === null || $this->jsonNode[$identifier] === null) {
            return null;
        }
        return new self($this->jsonNode[$identifier] ?? null);
    }

    /**
     * @inheritDoc
     */
    public function getStringValue(): ?string {
        return $this->jsonNode !== null ? addcslashes($this->jsonNode, "\\\r\n") : null;
    }

    /**
     * @inheritDoc
     */
    public function getBooleanValue(): ?bool {
        return $this->jsonNode !== null ? (bool)$this->jsonNode : null;
    }

    /**
     * @inheritDoc
     */
    public function getIntegerValue(): ?int {
        return $this->jsonNode !== null ? (int)$this->jsonNode : null;
    }

    /**
     * @inheritDoc
     */
    public function getFloatValue(): ?float {
        return $this->jsonNode !== null ? (float)$this->jsonNode : null;
    }

    /**
     * @inheritDoc
     */
    public function getLongValue(): ?int {
        return $this->getIntegerValue();
    }

    /**
     * @inheritDoc
     */
    public function getUUIDValue(): ?string {
        return $this->getStringValue();
    }

    /**
     * @return array<Parsable>
     * @throws Exception
     */
    public function getCollectionOfObjectValues(string $type): ?array {
        if ($this->jsonNode === null) {
            return null;
        }
        return array_map(static function ($val) use($type) {
            return $val->getObjectValue($type);
        }, array_map(static function ($value) {
            return new JsonParseNode($value);
        }, $this->jsonNode));
    }

    /**
     * @inheritDoc
     * @throws Exception
     */
    public function getObjectValue(?string $type = null): ?Parsable {
        if ($this->jsonNode === null || $this->jsonNode === 'null') {
            return null;
        }
        if ($type === null){
            throw new RuntimeException("Invalid type $type provided.");
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
     * @throws Exception
     */
    public function getDateTimeOffsetValue(): ?DateTime {
        return ($this->jsonNode !== null) ? new DateTime($this->jsonNode) : null;
    }

    /**
     * @inheritDoc
     */
    public function getEnumValue(string $targetEnum): ?Enum{
        if ($this->jsonNode === null){
            return null;
        }
        if (!is_subclass_of($targetEnum, Enum::class)) {
            throw new InvalidArgumentException('Invalid enum provided.');
        }
        return new ($targetEnum)($this->jsonNode);
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

    /**
     * @inheritDoc
     * @throws Exception
     */
    public function getCollectionOfPrimitiveValues(?string $typeName): ?array {
        if ($this->jsonNode === null){
            return null;
        }
        return array_map(static function ($x) use ($typeName) {
            $type = empty($typeName) ? get_debug_type($x) : $typeName;
            return (new JsonParseNode($x))->getAnyValue($type);
        }, $this->jsonNode);
    }

    /**
     * @return mixed
     * @throws Exception
     */
    public function getAnyValue(string $type) {
        switch ($type){
            case 'bool':
                return $this->getBooleanValue();
            case 'string':
                return $this->getStringValue();
            case 'int':
                return $this->getIntegerValue();
            case 'float':
                return $this->getFloatValue();
            case 'null':
                return null;
            case 'array':
                return $this->getCollectionOfPrimitiveValues(null);
            case Date::class:
                return $this->getDateValue();
            case Time::class:
                return $this->getTimeValue();
            case Byte::class:
                return $this->getByteValue();
            default:
                if (is_subclass_of($type, Enum::class)){
                    return $this->getEnumValue($type);
                }
                if (is_subclass_of($type, Parsable::class)){
                    return $this->getObjectValue($type);
                }
                throw new InvalidArgumentException("Unable to decode type $type");
        }

    }

    /**
     * @inheritDoc
     * @throws Exception
     */
    public function getDateValue(): ?Date {
        return ($this->jsonNode !== null) ? new Date($this->jsonNode) : null;
    }

    /**
     * @inheritDoc
     * @throws Exception
     */
    public function getTimeValue(): ?Time {
        return ($this->jsonNode !== null) ? new Time($this->jsonNode) : null;
    }

    /**
     * @inheritDoc
     */
    public function getByteValue(): ?Byte {
        return ($this->jsonNode !== null) ? new Byte($this->jsonNode) : null;
    }

    /**
     * @inheritDoc
     * @throws Exception
     */
    public function getDateTimeValue(): ?DateTime {
        return ($this->jsonNode !== null) ? new DateTime($this->jsonNode) : null;
    }

    /**
     * @inheritDoc
     * @throws Exception
     */
    public function getDateIntervalValue(): ?DateInterval{
        return ($this->jsonNode !== null) ? new DateInterval($this->jsonNode) : null;
    }
}