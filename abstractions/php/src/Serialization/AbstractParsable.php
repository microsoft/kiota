<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

use Closure;

abstract class AbstractParsable {

    /**
     * @return array<string, Closure>
     */
    abstract public function getFieldDeserializers(): array;

    /**
     * @param AbstractSerializationWriter $writer
     */
    abstract public function serialize(AbstractSerializationWriter $writer): void;

    /**
     * @var array
     */
    public array $additionalData;
}
