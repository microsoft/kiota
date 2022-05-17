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
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Types\Byte;
use Microsoft\Kiota\Abstractions\Types\Date;
use Microsoft\Kiota\Abstractions\Types\Time;
use Psr\Http\Message\StreamInterface;

/**
 * Class TextParseNode
 *
 * Parses text/plain content into various primitive and custom types
 *
 * @package Microsoft\Kiota\Serialization\Text
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 */
class TextParseNode implements ParseNode
{
    /**
     * @var string Content of the root node
     */
    private string $content;

    /**
     * @var callable|null
     */
    private $onBeforeAssignFieldValues = null;
    /**
     * @var callable|null
     */
    private $onAfterAssignFieldValues = null;

    const NO_STRUCTURED_DATA_ERR_MSG = "Text does not support structured data";

    /**
     * Initialises a TextParseNode
     * @param string $content non-empty string content
     */
    public function __construct(string $content)
    {
        if (!$content) {
            throw new \InvalidArgumentException('Content should be a non-empty string');
        }
        $this->content = $content;
    }

    /**
     * @inheritDoc
     */
    public function getChildNode(string $identifier): ?ParseNode
    {
        throw new \RuntimeException(self::NO_STRUCTURED_DATA_ERR_MSG);
    }

    /**
     * @inheritDoc
     */
    public function getStringValue(): ?string
    {
        return $this->content;
    }

    /**
     * @inheritDoc
     */
    public function getBooleanValue(): ?bool
    {
        return (bool) $this->content;
    }

    /**
     * @inheritDoc
     */
    public function getIntegerValue(): ?int
    {
        return (int) $this->content;
    }

    /**
     * @inheritDoc
     */
    public function getFloatValue(): ?float
    {
        return (float) $this->content;
    }

    /**
     * @inheritDoc
     */
    public function getObjectValue(string $type): ?Parsable
    {
        throw new \RuntimeException(self::NO_STRUCTURED_DATA_ERR_MSG);
    }

    /**
     * @inheritDoc
     */
    public function getCollectionOfObjectValues(string $type): ?array
    {
        throw new \RuntimeException(self::NO_STRUCTURED_DATA_ERR_MSG);
    }

    /**
     * @inheritDoc
     */
    public function getCollectionOfPrimitiveValues(?string $typeName = null): ?array
    {
        throw new \RuntimeException(self::NO_STRUCTURED_DATA_ERR_MSG);
    }

    /**
     * @inheritDoc
     * @throws \Exception
     */
    public function getDateTimeValue(): ?DateTime
    {
        return new DateTime($this->content);
    }

    /**
     * @inheritDoc
     * @throws \Exception
     */
    public function getDateIntervalValue(): ?DateInterval
    {
        return new DateInterval($this->content);
    }

    /**
     * @inheritDoc
     * @throws \Exception
     */
    public function getDateValue(): ?Date
    {
        return new Date($this->content);
    }

    /**
     * @inheritDoc
     * @throws \Exception
     */
    public function getTimeValue(): ?Time
    {
        return new Time($this->content);
    }

    /**
     * @inheritDoc
     */
    public function getEnumValue(string $targetEnum): ?Enum
    {
        if (!$targetEnum || !is_subclass_of($targetEnum, Enum::class)) {
            throw new \InvalidArgumentException("Target enum must extend ".Enum::class);
        }
        return new $targetEnum($this->content);
    }

    /**
     * @inheritDoc
     */
    public function getByteValue(): ?Byte
    {
        return new Byte((int)$this->content);
    }

    /**
     * @inheritDoc
     */
    public function getBinaryContent(): ?StreamInterface
    {
        return Utils::streamFor($this->content);
    }

    /**
     * @inheritDoc
     */
    public function getOnBeforeAssignFieldValues(): ?callable
    {
        return $this->onBeforeAssignFieldValues;
    }

    /**
     * @inheritDoc
     */
    public function getOnAfterAssignFieldValues(): ?callable
    {
        return $this->onAfterAssignFieldValues;
    }

    /**
     * @inheritDoc
     */
    public function setOnAfterAssignFieldValues(callable $value): void
    {
        $this->onAfterAssignFieldValues = $value;
    }

    /**
     * @inheritDoc
     */
    public function setOnBeforeAssignFieldValues(callable $value): void
    {
        $this->onBeforeAssignFieldValues = $value;
    }
}
