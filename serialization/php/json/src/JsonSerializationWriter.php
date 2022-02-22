<?php

namespace Microsoft\Kiota\Serialization\Json;

use DateInterval;
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

    private function writePropertyName(string $propertyName): void {
        $this->writer []= "\"{$propertyName}\":";
    }

    /**
     * @inheritDoc
     */
    public function writeStringValue(?string $key, ?string $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $propertyValue = $value !== null ? '"'.addcslashes($value, "\\\r\n\"").'"' : 'null';
        $this->writePropertyValue($key, $propertyValue);
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
        $this->writePropertyValue($key, $vV);
    }

    /**
     * @inheritDoc
     */
    public function writeFloatValue(?string $key, ?float $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue($key, $value);
    }

    /**
     * @inheritDoc
     */
    public function writeIntegerValue(?string $key, ?int $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue($key, $value);
    }

    /**
     * @inheritDoc
     */
    public function writeDateTimeValue(?string $key, ?DateTime $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        if ($value !== null) {
            $this->writePropertyValue($key, "\"{$value->format(DateTimeInterface::RFC3339)}Z\"");
        } else{
            $this->writePropertyValue($key, 'null');
        }
    }

    /**
     * @param string|null $key
     * @param Date|null $value
     * @return void
     */
    public function writeDateValue(?string $key, ?Date $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        if ($value !== null) {
            $valueString = (string)$value;
            $this->writePropertyValue($key, "\"{$valueString}\"");
        } else {
            $this->writePropertyValue($key, 'null');
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
            if ($key !== null) {
                $this->writer []= self::PROPERTY_SEPARATOR;
            }
        } else {
            $this->writePropertyValue($key, 'null');
        }
    }

    /**
     * @inheritDoc
     */
    public function writeObjectValue(?string $key, $value): void {
        if(!empty($key)) {
            $this->writePropertyName($key);
        }
        if ($value === null) {
            $this->writer []= 'null';
        } else {
            if ($this->onBeforeObjectSerialization !== null) {
                $this->onBeforeObjectSerialization($value);
            }
            $this->writer [] = '{';
            if ($this->onStartObjectSerialization !== null) {
                $this->onStartObjectSerialization($value, $this);
            }
            $value->serialize($this);
            if ($this->writer[count($this->writer) - 1] === ',') {
                array_pop($this->writer);
            }
            if ($this->onAfterObjectSerialization !== null) {
                $this->onAfterObjectSerialization($value);
            }
            $this->writer [] = '}';
        }
        if ($key !== null) {
            $this->writer [] = self::PROPERTY_SEPARATOR;
        }
    }

    /**
     * @inheritDoc
     */
    public function getSerializedContent(): StreamInterface {
        if (count($this->writer) > 0 && $this->writer[count($this->writer) - 1] === ','){
            array_pop($this->writer);
        }
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
            $this->writePropertyValue($key, 'null');
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
            $this->writePropertyValue($key, "\"{$value->value()}\"");
        } else {
            $this->writePropertyValue($key, 'null');
        }
    }

    /**
     * @inheritDoc
     */
    public function writeNullValue(?string $key): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writePropertyValue($key, 'null');
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
                $this->writeDateValue($key, $value);
                break;
            case Time::class:
                $this->writeTimeValue($key, $value);
                break;
            case Byte::class:
                $this->writeByteValue($key, $value);
                break;
            case DateTime::class:
                $this->writeDateTimeValue($key, $value);
                break;
            case DateInterval::class:
                $this->writeDateIntervalValue($key, $value);
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
                    if (is_subclass_of($value[0], Parsable::class)) {
                        $this->writeCollectionOfObjectValues($key, $value);
                    } else{
                        $this->writeCollectionOfNonParsableObjectValues($key, $value);
                    }
                }
                break;
            default:
                if (is_a($value, Parsable::class)) {
                    $this->writeObjectValue($key, $value);
                } else if(is_subclass_of($type, Enum::class)){
                    $this->writeEnumValue($key, $value);
                } else if(is_subclass_of($type, DateTimeInterface::class)){
                    $this->writeDateTimeValue($key, $value);
                } else if(is_a($value, StreamInterface::class) || is_subclass_of($type, StreamInterface::class)) {
                    $this->writeStringValue($key, $value->getContents());
                } else {
                   throw new RuntimeException("Could not serialize the object of type {$type}");
                }
                break;
        }
    }

    /**
     * @param string|null $key
     * @param mixed $value
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
        if ($key !== null) {
            $this->writer [] = self::PROPERTY_SEPARATOR;
        }
    }

    /**
     * @param string|null $key
     * @param mixed $value
     * @return void
     */
    private function writePropertyValue(?string $key, $value): void {
        $this->writer []= $value;

        if ($key !== null) {
            $this->writer []= self::PROPERTY_SEPARATOR;
        }
    }

    /**
     * @param string|null $key
     * @param array<mixed> $values
     * @return void
     */
    public function writeCollectionOfNonParsableObjectValues(?string $key, array $values): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }
        $this->writer []= '[';
        foreach ($values as $value){
            $this->writeAnyValue(null, $value);
            $this->writer [] = self::PROPERTY_SEPARATOR;
        }
        if (count($values) > 0){
            array_pop($this->writer);
        }
        $this->writer []= ']';
        if ($key !== null) {
            $this->writer [] = self::PROPERTY_SEPARATOR;
        }
    }

    /**
     * @inheritDoc
     */
    public function writeTimeValue(?string $key, ?Time $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }

        $val = $value !== null ? "\"{$value}\"" : 'null';
        $this->writePropertyValue($key, $val);
    }

    public function writeDateIntervalValue(?string $key, ?DateInterval $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }

        $res = null;
        if ($value !== null){
            $res = "P{$value->y}Y{$value->y}M{$value->d}DT{$value->h}H{$value->i}M{$value->s}S";
        }
        $val = $res !== null ? "\"{$res}\"" : 'null';
        $this->writePropertyValue($key, $val);
    }

    /**
     * @inheritDoc
     */
    public function writeByteValue(?string $key, ?Byte $value): void {
        if (!empty($key)) {
            $this->writePropertyName($key);
        }

        $val = $value !== null ? (int)(string)($value) : 'null';
        $this->writePropertyValue($key, $val);
    }
}