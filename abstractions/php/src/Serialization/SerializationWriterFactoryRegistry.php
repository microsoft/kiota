<?php


namespace Microsoft\Kiota\Abstractions\Serialization;


use UnexpectedValueException;

class SerializationWriterFactoryRegistry implements SerializationWriterFactoryInterface {

    /**
     * @var array<string, SerializationWriterFactoryInterface>
     */
    public array $contentTypeAssociatedFactories = [];

    /**
     * @param string $contentType
     * @return AbstractSerializationWriter
     * @throws UnexpectedValueException
     */
    public function getSerializationWriter(string $contentType): AbstractSerializationWriter {
        if (trim($contentType) === '') {
            throw new \InvalidArgumentException('contentType cannot be empty');
        }

        if (array_key_exists($contentType, $this->contentTypeAssociatedFactories)) {
            return $this->contentTypeAssociatedFactories[$contentType]->getSerializationWriter($contentType);
        }
        throw new UnexpectedValueException('Content type ' . $contentType . ' does not have a factory to be parsed');
    }
}