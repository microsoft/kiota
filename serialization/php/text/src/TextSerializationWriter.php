<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Serialization\Text;


use DateInterval;
use DateTime;
use GuzzleHttp\Psr7\Utils;
use Microsoft\Kiota\Abstractions\Enum;
use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;
use Microsoft\Kiota\Abstractions\Types\Byte;
use Microsoft\Kiota\Abstractions\Types\Date;
use Microsoft\Kiota\Abstractions\Types\Time;
use Psr\Http\Message\StreamInterface;

/**
 * Class TextSerializationWriter
 * @package Microsoft\Kiota\Serialization\Text
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 */
class TextSerializationWriter implements SerializationWriter
{
    /**
     * @var StreamInterface
     */
    private StreamInterface $stream;

    /**
     * @var callable|null
     */
    private $onBeforeObjectSerialization = null;
    /**
     * @var callable|null
     */
    private $onAfterObjectSerialization = null;
    /**
     * @var callable|null
     */
    private $onStartObjectSerialization = null;

    /**
     * Create a TextSerializationWriter
     */
    public function __construct()
    {
        $this->stream = Utils::streamFor();
    }

    public function __destruct()
    {
        $this->stream->close();
    }

    /**
     * @inheritDoc
     */
    public function writeStringValue(?string $key, ?string $value): void
    {
        if ($key) {
            throw new \InvalidArgumentException('Keys not supported for text/plain content type');
        }
        if ($this->stream->getSize()) {
            throw new \RuntimeException('A value was already written for this serialization writer. Text content only allows a single value');
        }
        (!$value) ? $this->stream->write('') : $this->stream->write($value);
    }

    /**
     * @inheritDoc
     */
    public function writeBooleanValue(?string $key, ?bool $value): void
    {
        ($value) ? $this->writeStringValue($key, 'true') : $this->writeStringValue($key, 'false');
    }

    /**
     * @inheritDoc
     */
    public function writeFloatValue(?string $key, ?float $value): void
    {
        $this->writeStringValue($key, (string) $value);
    }

    /**
     * @inheritDoc
     */
    public function writeIntegerValue(?string $key, ?int $value): void
    {
        $this->writeStringValue($key, (string) $value);
    }

    /**
     * @inheritDoc
     */
    public function writeDateTimeValue(?string $key, ?DateTime $value): void
    {
        if ($value) {
            $this->writeStringValue($key, $value->format(\DateTimeInterface::RFC3339));
        }
    }

    /**
     * @inheritDoc
     */
    public function writeCollectionOfObjectValues(?string $key, ?array $values): void
    {
        throw new \RuntimeException(TextParseNode::NO_STRUCTURED_DATA_ERR_MSG);
    }

    /**
     * @inheritDoc
     */
    public function writeObjectValue(?string $key, ?Parsable $value): void
    {
        throw new \RuntimeException(TextParseNode::NO_STRUCTURED_DATA_ERR_MSG);
    }

    /**
     * @inheritDoc
     */
    public function getSerializedContent(): StreamInterface
    {
        $this->stream->rewind();
        return $this->stream;
    }

    /**
     * @inheritDoc
     */
    public function writeEnumSetValue(?string $key, ?array $values): void
    {
        throw new \RuntimeException(TextParseNode::NO_STRUCTURED_DATA_ERR_MSG);
    }

    /**
     * @inheritDoc
     */
    public function writeEnumValue(?string $key, ?Enum $value): void
    {
        if ($value) {
            $this->writeStringValue($key, $value->value());
        }
    }

    /**
     * @inheritDoc
     */
    public function writeNullValue(?string $key): void
    {
        $this->writeStringValue($key, 'null');
    }

    /**
     * @inheritDoc
     */
    public function writeAdditionalData(?array $value): void
    {
        throw new \RuntimeException(TextParseNode::NO_STRUCTURED_DATA_ERR_MSG);
    }

    /**
     * @inheritDoc
     */
    public function writeDateValue(?string $key, ?Date $value): void
    {
        $this->writeStringValue($key, (string) $value);
    }

    /**
     * @inheritDoc
     */
    public function writeTimeValue(?string $key, ?Time $value): void
    {
        $this->writeStringValue($key, (string) $value);
    }

    /**
     * @inheritDoc
     */
    public function writeDateIntervalValue(?string $key, ?DateInterval $value): void
    {
        if ($value) {
            $valueString = "P{$value->y}Y{$value->y}M{$value->d}DT{$value->h}H{$value->i}M{$value->s}S";
            $this->writeStringValue($key, $valueString);
        }
    }

    /**
     * @inheritDoc
     */
    public function writeCollectionOfPrimitiveValues(?string $key, ?array $value): void
    {
        throw new \RuntimeException(TextParseNode::NO_STRUCTURED_DATA_ERR_MSG);
    }

    /**
     * @inheritDoc
     */
    public function writeAnyValue(?string $key, $value): void
    {
        throw new \RuntimeException(TextParseNode::NO_STRUCTURED_DATA_ERR_MSG);
    }

    /**
     * @inheritDoc
     */
    public function writeBinaryContent(?string $key, ?StreamInterface $value): void
    {
        if ($value) {
            $this->writeStringValue($key, $value->getContents());
        }
    }

    /**
     * @inheritDoc
     */
    public function setOnBeforeObjectSerialization(?callable $value): void
    {
        $this->onBeforeObjectSerialization = $value;
    }

    /**
     * @inheritDoc
     */
    public function getOnBeforeObjectSerialization(): ?callable
    {
        return $this->onBeforeObjectSerialization;
    }

    /**
     * @inheritDoc
     */
    public function setOnAfterObjectSerialization(?callable $value): void
    {
        $this->onAfterObjectSerialization = $value;
    }

    /**
     * @inheritDoc
     */
    public function getOnAfterObjectSerialization(): ?callable
    {
        return $this->onAfterObjectSerialization;
    }

    /**
     * @inheritDoc
     */
    public function setOnStartObjectSerialization(?callable $value): void
    {
        $this->onStartObjectSerialization = $value;
    }

    /**
     * @inheritDoc
     */
    public function getOnStartObjectSerialization(): ?callable
    {
        return $this->onStartObjectSerialization;
    }

    public function writeByteValue(?string $key, ?Byte $value): void
    {
        $this->writeStringValue($key, (string) $value);
    }
}
