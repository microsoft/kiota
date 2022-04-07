<?php

namespace Microsoft\Kiota\Serialization\Tests\Samples;

use Microsoft\Kiota\Abstractions\Serialization\AdditionalDataHolder;
use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;

class Address implements Parsable, AdditionalDataHolder
{
    /** @var array<string,mixed> $additionalData */
    private array $additionalData = [];
    private ?string $street = null;
    private ?string $city = null;

    /**
     * @inheritDoc
     */
    public function getFieldDeserializers(): array {
        $o = $this;
        return [
            "street" => static function (ParseNode $n) use ($o) {$o->setStreet($n->getStringValue());},
            "city" => function (ParseNode $n) use ($o) {$o->setCity($n->getStringValue());},
        ];
    }

    /**
     * @inheritDoc
     */
    public function serialize(SerializationWriter $writer): void {
        $writer->writeStringValue('street', $this->street);
        $writer->writeStringValue('city', $this->city);
    }

    public static function createFromDiscriminatorValue(ParseNode $parseNode): Address {
        return new self();
    }

    /**
     * @inheritDoc
     */
    public function getAdditionalData(): array {
        return $this->additionalData;
    }

    /**
     * @inheritDoc
     */
    public function setAdditionalData(array $value): void {
        $this->additionalData = $value;
    }

    /**
     * @return string|null
     */
    public function getStreet(): ?string {
        return $this->street;
    }

    /**
     * @param string|null $street
     */
    public function setStreet(?string $street): void {
        $this->street = $street;
    }

    /**
     * @return string|null
     */
    public function getCity(): ?string {
        return $this->city;
    }

    /**
     * @param string|null $city
     */
    public function setCity(?string $city): void {
        $this->city = $city;
    }


}