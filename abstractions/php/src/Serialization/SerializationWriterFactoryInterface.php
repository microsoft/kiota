<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

interface SerializationWriterFactoryInterface {
     public function getSerializationWriter(string $contentType): AbstractSerializationWriter;
}
