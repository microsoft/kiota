<?php

namespace Microsoft\Kiota\Serialization\Tests\Samples;

use Microsoft\Kiota\Abstractions\Serialization\AdditionalDataHolder;
use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;

class Person implements Parsable, AdditionalDataHolder
{
    /** @var array<string, mixed> */
    private array $additionalData = [];
    private ?string $name = null;

    private ?int $age = null;
    private ?float $height = null;

    private ?Address $address = null;

    private ?MaritalStatus $maritalStatus = null;
    /**
     * @inheritDoc
     */
    public function getFieldDeserializers(): array
    {
        $currentObject = $this;
        return [
            "name" => static function (ParseNode $n) use ($currentObject) {$currentObject->setName($n->getStringValue());},
            "age" => function (ParseNode $n) use ($currentObject) {$currentObject->setAge($n->getIntegerValue());},
            "height" => function (ParseNode $n) use ($currentObject) {$currentObject->setHeight($n->getFloatValue());},
            "maritalStatus" => function (ParseNode $n) use ($currentObject) {$currentObject->setMaritalStatus($n->getEnumValue(MaritalStatus::class));},
            "address" => function (ParseNode $n) use ($currentObject) {$currentObject->setAddress($n->getObjectValue(array(Address::class, 'createFromDiscriminatorValue')));}
        ];
    }

    /**
     * @inheritDoc
     */
    public function serialize(SerializationWriter $writer): void {
        $writer->writeStringValue('name', $this->name);
        $writer->writeIntegerValue('age', $this->age);
        $writer->writeEnumValue('maritalStatus', $this->maritalStatus);
        $writer->writeFloatValue('height', $this->height);
        $writer->writeObjectValue('address', $this->address);
    }

    /**
     * @inheritDoc
     */
    public function getAdditionalData(): array {
        return $this->additionalData;
    }

    public static function createFromDiscriminatorValue(ParseNode $parseNode): Person {
        return new self();
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
    public function getName(): ?string {
        return $this->name;
    }

    /**
     * @param string|null $name
     */
    public function setName(?string $name): void {
        $this->name = $name;
    }

    /**
     * @return int|null
     */
    public function getAge(): ?int {
        return $this->age;
    }

    /**
     * @param int|null $age
     */
    public function setAge(?int $age): void {
        $this->age = $age;
    }

    /**
     * @return float|null
     */
    public function getHeight(): ?float {
        return $this->height;
    }

    /**
     * @param float|null $height
     */
    public function setHeight(?float $height): void {
        $this->height = $height;
    }

    /**
     * @param MaritalStatus|null $maritalStatus
     */
    public function setMaritalStatus(?MaritalStatus $maritalStatus): void {
        $this->maritalStatus = $maritalStatus;
    }

    /**
     * @return MaritalStatus|null
     */
    public function getMaritalStatus(): ?MaritalStatus {
        return $this->maritalStatus;
    }

    /**
     * @return Address|null
     */
    public function getAddress(): ?Address {
        return $this->address;
    }

    /**
     * @param Address|null $address
     */
    public function setAddress(?Address $address): void {
        $this->address = $address;
    }

}