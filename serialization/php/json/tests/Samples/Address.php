<?php

namespace Microsoft\Kiota\Serialization\Tests\Samples;

use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;

class Address implements Parsable
{
    /** @var array<string,mixed> $additionalData */
    private array $additionalData = [];
    private ?string $street = null;
    private ?string $city = null;

    /**
     * @inheritDoc
     */
    public function getFieldDeserializers(): array {
        return [
            "street" => static function (self $o, ParseNode $n) {$o->setStreet($n->getStringValue());},
            "city" => function (self $o, ParseNode $n) {$o->setCity($n->getStringValue());},
        ];
    }

    /**
     * @inheritDoc
     */
    public function serialize(SerializationWriter $writer): void {
        $writer->writeStringValue('street', $this->street);
        $writer->writeStringValue('city', $this->city);
    }

    /**
     * @inheritDoc
     */
    public function getAdditionalData(): ?array {
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