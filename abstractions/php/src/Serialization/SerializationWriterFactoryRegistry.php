<?php


namespace Microsoft\Kiota\Abstractions\Serialization;


use UnexpectedValueException;

class SerializationWriterFactoryRegistry implements SerializationWriterFactory {

    /**
     * @var array<string, SerializationWriterFactory>
     */
    public array $contentTypeAssociatedFactories = [];

    /**
     * @param string $contentType
     * @return SerializationWriter
     * @throws UnexpectedValueException
     */
    public function getSerializationWriter(string $contentType): SerializationWriter {
        if (trim($contentType) === '') {
            throw new \InvalidArgumentException('contentType cannot be empty');
        }

        if (array_key_exists($contentType, $this->contentTypeAssociatedFactories)) {
            return $this->contentTypeAssociatedFactories[$contentType]->getSerializationWriter($contentType);
        }
        throw new UnexpectedValueException('Content type ' . $contentType . ' does not have a factory to be parsed');
    }

    public function getValidContentType(): string {
        throw new \RuntimeException("The registry supports multiple content types. Get the registered factory instead.");
    }

    public static function getDefaultInstance(): SerializationWriterFactoryRegistry {
        return new self();
    }
}