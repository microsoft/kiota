<?php

namespace Microsoft\Kiota\Serialization\Json;

use DateTime;
use DateTimeInterface;
use GuzzleHttp\Psr7\Utils;
use Microsoft\Kiota\Abstractions\Enum;
use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;
use Psr\Http\Message\StreamInterface;

/**
 * @method onBeforeObjectSerialization(Parsable $value);
 * @method onStartObjectSerialization(Parsable $value, SerializationWriter $writer);
 * @method onAfterObjectSerialization(Parsable $value);
 */
class JsonSerializationWriter implements SerializationWriter
{
    /** @var array<mixed> $writer */
    private array $writer = [];

    /** @var string PROPERTY_SEPARATOR */
    private const PROPERTY_SEPARATOR = ',';

    /** @var callable|null $onStartObjectSerialization */
    private $onStartObjectSerialization;

    /** @var callable|null $onAfterObjectSerialization */
    private $onAfterObjectSerialization;

    /** @var callable|null $onBeforeObjectSerialization */
    private $onBeforeObjectSerialization;

    public function writePropertyName(string $propertyName): void {
        $this->writer []= "\"{$propertyName}\":";
    }
    /**
     * @inheritDoc
     */
    public function writeStringValue(?string $key, string $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue("\"$value\"");
    }

    /**
     * @inheritDoc
     */
    public function writeBooleanValue(?string $key, bool $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue($value);
    }

    /**
     * @inheritDoc
     */
    public function writeFloatValue(?string $key, float $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue($value);
    }

    /**
     * @inheritDoc
     */
    public function writeIntegerValue(?string $key, int $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue($value);
    }

    /**
     * @inheritDoc
     */
    public function writeLongValue(?string $key, int $value): void {
        $this->writeIntegerValue($key, $value);
    }

    /**
     * @inheritDoc
     */
    public function writeUUIDValue(?string $key, string $value): void {
        $this->writeStringValue($key, $value);
    }

    /**
     * @inheritDoc
     */
    public function writeDateTimeOffsetValue(?string $key, DateTime $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue("\"{$value->format(DateTimeInterface::RFC3339)}Z\"");
    }

    /**
     * @inheritDoc
     */
    public function writeCollectionOfObjectValues(?string $key, array $values): void {
        if($key !== null){
            $this->writePropertyName($key);
        }
        $this->writer []= '[';
        foreach($values as $v) {
            $this->writeObjectValue(null, $v);
            $this->writer []= self::PROPERTY_SEPARATOR;
        }
        if(count($values) > 0) {
            array_pop($this->writer);
        }
        $this->writer []= ']';
        if($key !== null){
            $this->writer []= self::PROPERTY_SEPARATOR;
        }
    }

    /**
     * @inheritDoc
     */
    public function writeObjectValue(?string $key, $value): void {
        if(!empty($key)) {
            $this->writePropertyName($key);
        }
        if ($this->onBeforeObjectSerialization !== null) {
            $this->onBeforeObjectSerialization($value);
        }
        $this->writer []= '{';
        if ($this->onStartObjectSerialization !== null) {
            $this->onStartObjectSerialization($value, $this);
        }
        $value->serialize($this);
        if($this->onAfterObjectSerialization !== null) {
            $this->onAfterObjectSerialization($value);
        }
        $this->writer []= '}';
    }

    /**
     * @inheritDoc
     */
    public function getSerializedContent(): StreamInterface {
        return Utils::streamFor(implode('', $this->writer));
    }

    /**
     * @inheritDoc
     */
    public function writeEnumSetValue(?string $key, array $values): void {
        $vals = [];
        foreach ($values as $value){
            $vals []= $value->value();
        }
        $this->writeStringValue($key, implode(',', $vals));
    }

    /**
     * @inheritDoc
     */
    public function writeEnumValue(?string $key, Enum $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue("\"{$value->value()}\"");
    }

    /**
     * @inheritDoc
     */
    public function writeNullValue(?string $key): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue('null');
    }

    /**
     * @inheritDoc
     */
    public function writeAdditionalData(?array $value): void {
        if($value === null) {
            return;
        }
        foreach ($value as $key => $val) {
            $this->writeAnyValue($key, $val);
        }
    }

    /**
     * @inheritDoc
     */
    public function setOnBeforeObjectSerialization(callable $value): void {
        $this->onBeforeObjectSerialization = $value;
    }

    /**
     * @inheritDoc
     */
    public function getOnBeforeObjectSerialization(): callable {
        return $this->onBeforeObjectSerialization;
    }

    /**
     * @inheritDoc
     */
    public function setOnAfterObjectSerialization(callable $value): void {
        $this->onAfterObjectSerialization = $value;
    }

    /**
     * @inheritDoc
     */
    public function getOnAfterObjectSerialization(): callable {
        return $this->onAfterObjectSerialization;
    }

    /**
     * @inheritDoc
     */
    public function setOnStartObjectSerialization(callable $value): void {
        $this->onStartObjectSerialization = $value;
    }

    /**
     * @inheritDoc
     */
    public function getOnStartObjectSerialization(): callable {
        return $this->onStartObjectSerialization;
    }

    /**
     * @param string $key
     * @param mixed $value
     */
    public function writeAnyValue(string $key, $value): void{
        $type = gettype($value);

        switch ($type) {
            case 'double':
                $this->writeFloatValue($key, $value);
                break;
            case 'string':
                $this->writeStringValue($key, $value);
                break;
            case 'integer':
                $this->writeIntegerValue($key, $value);
                break;
            case 'boolean':
                $this->writeBooleanValue($key, $value);
                break;
            case 'object':
                $this->writeNonParsableObjectValue($key, $value);
                break;
            case 'array':
                $keys = array_filter(array_keys($value), 'is_string');
                // If there are string keys then that means this is a single
                // object we are dealing with
                // otherwise it is a collection of objects.
                if (!empty($keys)){
                    $this->writeNonParsableObjectValue($key, (object)$value);
                } else if (!empty($value)){
                    if (is_a($value[0], Parsable::class)) {
                        $this->writeCollectionOfObjectValues($key, $value);
                    } else{
                        $this->writeCollectionOfNonParsableObjectValues($key, $value);
                    }
                }
                break;
            default:
                if (is_a($value, Parsable::class)) {
                    $this->writeObjectValue($key, $value);
                }
                break;
        }
    }

    /**
     * @param string|null $key
     * @param object $value
     */
    public function writeNonParsableObjectValue(?string $key, object $value): void{
        if(!empty($key)) {
            $this->writePropertyName($key);
        }
        $value = (array)$value;
        $this->writer []= '{';
        foreach ($value as $keyV => $val) {
            $this->writeAnyValue($keyV, $val);
        }
        if (count($value) > 0){
            array_pop($this->writer);
        }
        $this->writer []= '}';
        $this->writer []= self::PROPERTY_SEPARATOR;
    }

    /**
     * @param mixed $value
     * @return void
     */
    private function writePropertyValue($value): void {
        $this->writer []= $value;
        $this->writer []= self::PROPERTY_SEPARATOR;
    }

    /**
     * @param string $key
     * @param array<mixed> $values
     * @return void
     */
    public function writeCollectionOfNonParsableObjectValues(string $key, array $values): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writer []= '[';
        foreach ($values as $value){
            $this->writeNonParsableObjectValue(null, (object)$value);
        }
        if (count($values) > 0){
            array_pop($this->writer);
        }
        $this->writer []= ']';
        $this->writer []= self::PROPERTY_SEPARATOR;
    }
}