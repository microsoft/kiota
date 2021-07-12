<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

use Closure;

abstract class Parsable {

    /**
     * @return array<string, Closure>
     */
    abstract public function getFieldDeserializers(): array;

    /**
     * @param SerializationWriter $writer
     */
    abstract public function serialize(SerializationWriter $writer): void;

    /**
     * @var array
     */
    public array $additionalData;
}
