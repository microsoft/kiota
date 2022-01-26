<?php

namespace Microsoft\Kiota\Serialization\Tests\Samples;

use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;

class FollowupFlag implements Parsable 
{
    /** @var array<string, mixed> $AdditionalData Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well. */
    private array $additionalData;
    
    /** @var DateTimeTimeZone|null $completedDateTime  */
    private ?DateTimeTimeZone $completedDateTime = null;
    
    /** @var DateTimeTimeZone|null $dueDateTime  */
    private ?DateTimeTimeZone $dueDateTime = null;
    
    /** @var FollowupFlagStatus|null $flagStatus  */
    private ?FollowupFlagStatus $flagStatus = null;
    
    /** @var DateTimeTimeZone|null $startDateTime  */
    private ?DateTimeTimeZone $startDateTime = null;
    
    /**
     * Instantiates a new followupFlag and sets the default values.
    */
    public function __construct() {
        $this->additionalData = [];
    }

    /**
     * Gets the AdditionalData property value. Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
     * @return array<string, mixed>
    */
    public function getAdditionalData(): array {
        return $this->additionalData;
    }

    /**
     * Gets the completedDateTime property value. 
     * @return DateTimeTimeZone|null
    */
    public function getCompletedDateTime(): ?DateTimeTimeZone {
        return $this->completedDateTime;
    }

    /**
     * Gets the dueDateTime property value. 
     * @return DateTimeTimeZone|null
    */
    public function getDueDateTime(): ?DateTimeTimeZone {
        return $this->dueDateTime;
    }

    /**
     * Gets the flagStatus property value. 
     * @return FollowupFlagStatus|null
    */
    public function getFlagStatus(): ?FollowupFlagStatus {
        return $this->flagStatus;
    }

    /**
     * Gets the startDateTime property value. 
     * @return DateTimeTimeZone|null
    */
    public function getStartDateTime(): ?DateTimeTimeZone {
        return $this->startDateTime;
    }

    /**
     * The deserialization information for the current model
     * @return array<string, callable>
    */
    public function getFieldDeserializers(): array {
        return  [
            'completedDateTime' => function (FollowupFlag $o, ParseNode $n) { $o->setCompletedDateTime($n->getObjectValue(DateTimeTimeZone::class)); },
            'dueDateTime' => function (FollowupFlag $o, ParseNode $n) { $o->setDueDateTime($n->getObjectValue(DateTimeTimeZone::class)); },
            'flagStatus' => function (FollowupFlag $o, ParseNode $n) { $o->setFlagStatus($n->getEnumValue(FollowupFlagStatus::class)); },
            'startDateTime' => function (FollowupFlag $o, ParseNode $n) { $o->setStartDateTime($n->getObjectValue(DateTimeTimeZone::class)); },
        ];
    }

    /**
     * Serializes information the current object
     * @param SerializationWriter $writer Serialization writer to use to serialize this model
    */
    public function serialize(SerializationWriter $writer): void {
        $writer->writeObjectValue('completedDateTime', $this->completedDateTime);
        $writer->writeObjectValue('dueDateTime', $this->dueDateTime);
        $writer->writeEnumValue('flagStatus', $this->flagStatus);
        $writer->writeObjectValue('startDateTime', $this->startDateTime);
        $writer->writeAdditionalData($this->additionalData);
    }

    /**
     * Sets the AdditionalData property value. Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
     *  @param array<string,mixed> $value Value to set for the AdditionalData property.
    */
    public function setAdditionalData(?array $value ): void {
        $this->additionalData = $value;
    }

    /**
     * Sets the completedDateTime property value. 
     *  @param DateTimeTimeZone|null $value Value to set for the completedDateTime property.
    */
    public function setCompletedDateTime(?DateTimeTimeZone $value ): void {
        $this->completedDateTime = $value;
    }

    /**
     * Sets the dueDateTime property value. 
     *  @param DateTimeTimeZone|null $value Value to set for the dueDateTime property.
    */
    public function setDueDateTime(?DateTimeTimeZone $value ): void {
        $this->dueDateTime = $value;
    }

    /**
     * Sets the flagStatus property value. 
     *  @param FollowupFlagStatus|null $value Value to set for the flagStatus property.
    */
    public function setFlagStatus(?FollowupFlagStatus $value ): void {
        $this->flagStatus = $value;
    }

    /**
     * Sets the startDateTime property value. 
     *  @param DateTimeTimeZone|null $value Value to set for the startDateTime property.
    */
    public function setStartDateTime(?DateTimeTimeZone $value ): void {
        $this->startDateTime = $value;
    }

}
