<?php

namespace Microsoft\Kiota\Serialization\Json;

use DateTime;
use DateTimeInterface;
use GuzzleHttp\Psr7\Utils;
use Microsoft\Kiota\Abstractions\Enum;
use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;
use Microsoft\Kiota\Abstractions\Types\Byte;
use Microsoft\Kiota\Abstractions\Types\Date;
use Microsoft\Kiota\Abstractions\Types\Time;
use Psr\Http\Message\StreamInterface;

/**
 * @method onBeforeObjectSerialization(?Parsable $value);
 * @method onStartObjectSerialization(?Parsable $value, SerializationWriter $writer);
 * @method onAfterObjectSerialization(?Parsable $value);
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
    public function writeStringValue(?string $key, ?string $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $propertyValue = $value !== null ? "\"$value\"" : 'null';
        $this->writePropertyValue($propertyValue);
    }

    /**
     * @inheritDoc
     */
    public function writeBooleanValue(?string $key, ?bool $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $valS = ['false', 'true'];
        $vV= $value === null ? 'null' : $valS[$value];
        $this->writePropertyValue($vV);
    }

    /**
     * @inheritDoc
     */
    public function writeFloatValue(?string $key, ?float $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue($value);
    }

    /**
     * @inheritDoc
     */
    public function writeIntegerValue(?string $key, ?int $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue($value);
    }

    /**
     * @inheritDoc
     */
    public function writeLongValue(?string $key, ?int $value): void {
        $this->writeIntegerValue($key, $value);
    }

    /**
     * @inheritDoc
     */
    public function writeUUIDValue(?string $key, ?string $value): void {
        $this->writeStringValue($key, $value);
    }

    /**
     * @inheritDoc
     */
    public function writeDateTimeOffsetValue(?string $key, ?DateTime $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        if ($value !== null) {
            $this->writePropertyValue("\"{$value->format(DateTimeInterface::RFC3339)}Z\"");
        } else{
            $this->writePropertyValue('null');
        }
    }

    /**
     * @param string|null $key
     * @param Date|null $value
     * @return void
     */
    public function writeDateOnlyValue(?string $key, ?Date $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        if ($value !== null) {
            $valueString = (string)$value;
            $this->writePropertyValue("\"{$valueString}\"");
        } else {
            $this->writePropertyValue('null');
        }
    }

    /**
     * @inheritDoc
     */
    public function writeCollectionOfObjectValues(?string $key, ?array $values): void {
        if($key !== null){
            $this->writePropertyName($key);
        }
        if ($values !== null) {
            $this->writer [] = '[';
            foreach ($values as $v) {
                $this->writeObjectValue(null, $v);
                $this->writer [] = self::PROPERTY_SEPARATOR;
            }
            if (count($values) > 0) {
                array_pop($this->writer);
            }
            $this->writer [] = ']';
        } else {
            $this->writePropertyValue('null');
        }
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
        if ($value !== null) {
            $value->serialize($this);
        }
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
    public function writeEnumSetValue(?string $key, ?array $values): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        if ($values !== null) {
            $valS = [];
            foreach ($values as $value){
                $valS []= $value->value();
            }
            $this->writeStringValue($key, implode(',', $valS));
        } else {
            $this->writePropertyValue('null');
        }
    }

    /**
     * @inheritDoc
     */
    public function writeEnumValue(?string $key, ?Enum $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        if ($value !== null) {
            $this->writePropertyValue("\"{$value->value()}\"");
        } else {
            $this->writePropertyValue('null');
        }
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
     * @throws \JsonException
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
    public function setOnBeforeObjectSerialization(?callable $value): void {
        $this->onBeforeObjectSerialization = $value;
    }

    /**
     * @inheritDoc
     */
    public function getOnBeforeObjectSerialization(): ?callable {
        return $this->onBeforeObjectSerialization;
    }

    /**
     * @inheritDoc
     */
    public function setOnAfterObjectSerialization(?callable $value): void {
        $this->onAfterObjectSerialization = $value;
    }

    /**
     * @inheritDoc
     */
    public function getOnAfterObjectSerialization(): ?callable {
        return $this->onAfterObjectSerialization;
    }

    /**
     * @inheritDoc
     */
    public function setOnStartObjectSerialization(?callable $value): void {
        $this->onStartObjectSerialization = $value;
    }

    /**
     * @inheritDoc
     */
    public function getOnStartObjectSerialization(): ?callable {
        return $this->onStartObjectSerialization;
    }

    /**
     * @param string|null $key
     * @param mixed $value
     * @throws \JsonException
     */
    public function writeAnyValue(?string $key, $value): void{
        $type = get_debug_type($value);
        switch ($type) {
            case 'float':
                $this->writeFloatValue($key, $value);
                break;
            case 'string':
                $this->writeStringValue($key, $value);
                break;
            case 'int':
                $this->writeIntegerValue($key, $value);
                break;
            case 'bool':
                $this->writeBooleanValue($key, $value);
                break;
            case 'null':
                $this->writeNullValue($key);
                break;
            case Date::class:
                $this->writeDateOnlyValue($key, $value);
                break;
            case Time::class:
                $this->writeTimeOnlyValue($key, $value);
                break;
            case Byte::class:
                $this->writeByteValue($key, $value);
                break;
            case DateTime::class:
                $this->writeDateTimeOffsetValue($key, $value);
                break;
            case 'stdClass':
                $this->writeNonParsableObjectValue($key, (object)$value);
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
     * @param mixed $value
     * @throws \JsonException
     */
    public function writeNonParsableObjectValue(?string $key, $value): void{
        if(!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writer []= '{';
        $value = (array)$value;
        foreach ($value as $kKey => $kVal) {
            $this->writeAnyValue($kKey, $kVal);
        }
        if (count($value) > 0) {
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
     * @param string|null $key
     * @param array<mixed> $values
     * @return void
     * @throws \JsonException
     */
    public function writeCollectionOfNonParsableObjectValues(?string $key, array $values): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writer []= '[';
        foreach ($values as $value){
            $this->writeAnyValue(null, $value);
        }
        if (count($values) > 0){
            array_pop($this->writer);
        }
        $this->writer []= ']';
        $this->writer []= self::PROPERTY_SEPARATOR;
    }

    /**
     * @inheritDoc
     */
    public function writeTimeOnlyValue(?string $key, ?Time $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }

        $val = $value !== null ? "\"{$value}\"" : 'null';
        $this->writePropertyValue($val);
    }

    /**
     * @inheritDoc
     */
    public function writeByteValue(?string $key, ?Byte $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }

        $val = $value !== null ? "\"{$value}\"" : 'null';
        $this->writePropertyValue($val);
    }
}