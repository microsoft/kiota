<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

abstract class Parsable {
    abstract public function getFieldDeserializers(): array;
    abstract public function serialize(SerializationWriter $writer): void;
    public array $additionalData;
}
