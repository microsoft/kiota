<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

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
}
