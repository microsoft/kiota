<?php

namespace Microsoft\Kiota\Serialization\Tests\Samples;

use DateTime;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;

class Attachment extends Entity
{
    /** @var string|null $contentType The MIME type. */
    private ?string $contentType = null;
    
    /** @var bool|null $isInline true if the attachment is an inline attachment; otherwise, false. */
    private ?bool $isInline = null;
    
    /** @var DateTime|null $lastModifiedDateTime The Timestamp type represents date and time information using ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z */
    private ?DateTime $lastModifiedDateTime = null;
    
    /** @var string|null $name The display name of the attachment. This does not need to be the actual file name. */
    private ?string $name = null;
    
    /** @var int|null $size The length of the attachment in bytes. */
    private ?int $size = null;
    
    /**
     * Instantiates a new attachment and sets the default values.
    */
    public function __construct() {
        parent::__construct();
    }

    /**
     * Gets the contentType property value. The MIME type.
     * @return string|null
    */
    public function getContentType(): ?string {
        return $this->contentType;
    }

    /**
     * Gets the isInline property value. true if the attachment is an inline attachment; otherwise, false.
     * @return bool|null
    */
    public function getIsInline(): ?bool {
        return $this->isInline;
    }

    /**
     * Gets the lastModifiedDateTime property value. The Timestamp type represents date and time information using ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z
     * @return DateTime|null
    */
    public function getLastModifiedDateTime(): ?DateTime {
        return $this->lastModifiedDateTime;
    }

    /**
     * Gets the name property value. The display name of the attachment. This does not need to be the actual file name.
     * @return string|null
    */
    public function getName(): ?string {
        return $this->name;
    }

    /**
     * Gets the size property value. The length of the attachment in bytes.
     * @return int|null
    */
    public function getSize(): ?int {
        return $this->size;
    }

    /**
     * The deserialization information for the current model
     * @return array<string, callable>
    */
    public function getFieldDeserializers(): array {
        return array_merge(parent::getFieldDeserializers(), [
            'contentType' => function (self $o, ParseNode $n) { $o->setContentType($n->getStringValue()); },
            'isInline' => function (self $o, ParseNode $n) { $o->setIsInline($n->getBooleanValue()); },
            'lastModifiedDateTime' => function (self $o, ParseNode $n) { $o->setLastModifiedDateTime($n->getDateTimeOffsetValue()); },
            'name' => function (self $o, ParseNode $n) { $o->setName($n->getStringValue()); },
            'size' => function (self $o, ParseNode $n) { $o->setSize($n->getIntegerValue()); },
        ]);
    }

    /**
     * Serializes information the current object
     * @param SerializationWriter $writer Serialization writer to use to serialize this model
    */
    public function serialize(SerializationWriter $writer): void {
        parent::serialize($writer);
        $writer->writeStringValue('contentType', $this->contentType);
        $writer->writeBooleanValue('isInline', $this->isInline);
        $writer->writeDateTimeOffsetValue('lastModifiedDateTime', $this->lastModifiedDateTime);
        $writer->writeStringValue('name', $this->name);
        $writer->writeIntegerValue('size', $this->size);
    }

    /**
     * Sets the contentType property value. The MIME type.
     *  @param string|null $value Value to set for the contentType property.
    */
    public function setContentType(?string $value ): void {
        $this->contentType = $value;
    }

    /**
     * Sets the isInline property value. true if the attachment is an inline attachment; otherwise, false.
     *  @param bool|null $value Value to set for the isInline property.
    */
    public function setIsInline(?bool $value ): void {
        $this->isInline = $value;
    }

    /**
     * Sets the lastModifiedDateTime property value. The Timestamp type represents date and time information using ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z
     *  @param DateTime|null $value Value to set for the lastModifiedDateTime property.
    */
    public function setLastModifiedDateTime(?DateTime $value ): void {
        $this->lastModifiedDateTime = $value;
    }

    /**
     * Sets the name property value. The display name of the attachment. This does not need to be the actual file name.
     *  @param string|null $value Value to set for the name property.
    */
    public function setName(?string $value ): void {
        $this->name = $value;
    }

    /**
     * Sets the size property value. The length of the attachment in bytes.
     *  @param int|null $value Value to set for the size property.
    */
    public function setSize(?int $value ): void {
        $this->size = $value;
    }

}
