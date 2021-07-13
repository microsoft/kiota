<?php


namespace Microsoft\Kiota\Abstractions\Serialization;


class SerializationWriterFactoryRegistry implements SerializationWriterFactory {

    /**
     * @var array<string, SerializationWriterFactory>
     */
    public array $contentTypeAssociatedFactories = [];

    public function getSerializationWriter(?string $contentType): SerializationWriter {
        if (is_null($contentType)) {
            throw new \InvalidArgumentException('parameter contentType cannot be null');
        }
        if (trim($contentType) === '') {
            throw new \InvalidArgumentException('contentType cannot be empty');
        }

        if (array_key_exists($contentType, $this->contentTypeAssociatedFactories)) {
            return $this->contentTypeAssociatedFactories[$contentType]->getSerializationWriter($contentType);
        }
        throw new \UnexpectedValueException('Content type ' . $contentType . ' does not have a factory to be parsed');
    }
}