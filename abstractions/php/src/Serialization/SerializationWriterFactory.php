<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

interface SerializationWriterFactory {

    /**
     * Creates a new SerializationWriter instance for the given content type.
     * @param string $contentType the content type to create a serialization writer for.
     * @return SerializationWriter a new SerializationWriter instance for the given content type.
     */
     public function getSerializationWriter(string $contentType): SerializationWriter;

    /**
     * Gets the content type this factory creates serialization writers for.
     * @return string the content type this factory creates serialization writers for.
     */
     public function getValidContentType(): string;
}
