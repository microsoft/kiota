<?php

namespace Microsoft\Kiota\Serialization\Tests\Samples;

use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;

class DateTimeTimeZone implements Parsable 
{
    /** @var array<string, mixed>|null $AdditionalData Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well. */
    private ?array $additionalData;
    
    /** @var string|null $dateTime A single point of time in a combined date and time representation ({date}T{time}). For example, '2019-04-16T09:00:00'. */
    private ?string $dateTime;
    
    /** @var string|null $timeZone Represents a time zone, for example, 'Pacific Standard Time'. See below for possible values. */
    private ?string $timeZone;
    
    /**
     * Instantiates a new dateTimeTimeZone and sets the default values.
    */
    public function __construct() {
        $this->additionalData = [];
    }

    /**
     * Gets the AdditionalData property value. Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
     * @return array<string, mixed>|mixed
    */
    public function getAdditionalData(): ?array {
        return $this->additionalData;
    }

    /**
     * Gets the dateTime property value. A single point of time in a combined date and time representation ({date}T{time}). For example, '2019-04-16T09:00:00'.
     * @return string|null
    */
    public function getDateTime(): ?string {
        return $this->dateTime;
    }

    /**
     * Gets the timeZone property value. Represents a time zone, for example, 'Pacific Standard Time'. See below for possible values.
     * @return string|null
    */
    public function getTimeZone(): ?string {
        return $this->timeZone;
    }

    /**
     * The deserialization information for the current model
     * @return array<string, callable>
    */
    public function getFieldDeserializers(): array {
        return  [
            'dateTime' => function (DateTimeTimeZone $o, ParseNode $n) { $o->setDateTime($n->getStringValue()); },
            'timeZone' => function (DateTimeTimeZone $o, ParseNode $n) { $o->setTimeZone($n->getStringValue()); },
        ];
    }

    /**
     * Serializes information the current object
     * @param SerializationWriter $writer Serialization writer to use to serialize this model
    */
    public function serialize(SerializationWriter $writer): void {
        $writer->writeStringValue('dateTime', $this->dateTime);
        $writer->writeStringValue('timeZone', $this->timeZone);
        $writer->writeAdditionalData($this->additionalData);
    }

    /**
     * Sets the AdditionalData property value. Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
     *  @param array<string,mixed>|null $value Value to set for the AdditionalData property.
    */
    public function setAdditionalData(?array $value ): void {
        $this->additionalData = $value;
    }

    /**
     * Sets the dateTime property value. A single point of time in a combined date and time representation ({date}T{time}). For example, '2019-04-16T09:00:00'.
     *  @param string|null $value Value to set for the dateTime property.
    */
    public function setDateTime(?string $value ): void {
        $this->dateTime = $value;
    }

    /**
     * Sets the timeZone property value. Represents a time zone, for example, 'Pacific Standard Time'. See below for possible values.
     *  @param string|null $value Value to set for the timeZone property.
    */
    public function setTimeZone(?string $value ): void {
        $this->timeZone = $value;
    }

}
