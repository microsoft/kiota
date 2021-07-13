<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

interface SerializationWriterFactory {
     public function getSerializationWriter(?string $contentType): SerializationWriter;
}
