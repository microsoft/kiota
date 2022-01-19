<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

use Closure;

/**
 * Defines the serializable model object.
 */
interface Parsable {

    /**
     * Gets the deserialization information for this object.
     * @return array<string, callable> The deserialization information for this object where each entry is a property key with its deserialization callback.
     */
    public function getFieldDeserializers(): array;

    /**
     * Writes the objects properties to the current writer.
     * @param SerializationWriter $writer The writer to write to.
     */
    public function serialize(SerializationWriter $writer): void;

    /**
     * Gets the additional data for this object that did not belong to the properties.
     * @return array<string, mixed> The additional data for this object.
     */
    public function getAdditionalData(): array;

    /**
     * Sets the additional data for this object that did not belong to the properties.
     * @param array<string, mixed> $value The additional data for this object.
     */
    public function setAdditionalData(array $value): void;
}
