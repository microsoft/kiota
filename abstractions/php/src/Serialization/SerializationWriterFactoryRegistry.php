<?php


namespace Microsoft\Kiota\Abstractions\Serialization;


use InvalidArgumentException;
use RuntimeException;
use UnexpectedValueException;

class SerializationWriterFactoryRegistry implements SerializationWriterFactory {

    /**
     * List of factories that are registered by content type
     * @var array<string, SerializationWriterFactory>
     */
    public array $contentTypeAssociatedFactories = [];

    /**
     * Default singleton instance of the registry to be used when registering new factories that should be available by default.
     * @var SerializationWriterFactoryRegistry|null
     */
    private static ?SerializationWriterFactoryRegistry $defaultInstance = null;

    /**
     * @param string $contentType
     * @return SerializationWriter
     * @throws UnexpectedValueException
     */
    public function getSerializationWriter(string $contentType): SerializationWriter {
        if (trim($contentType) === '') {
            throw new InvalidArgumentException('contentType cannot be empty');
        }

        if (array_key_exists($contentType, $this->contentTypeAssociatedFactories)) {
            return $this->contentTypeAssociatedFactories[$contentType]->getSerializationWriter($contentType);
        }
        throw new UnexpectedValueException('Content type ' . $contentType . ' does not have a factory to be parsed');
    }

    public function getValidContentType(): string {
        throw new RuntimeException("The registry supports multiple content types. Get the registered factory instead.");
    }

    /**
     * Gets the singleton default instance of  @link SerializationWriterFactoryRegistry
     * @return SerializationWriterFactoryRegistry
     */
    public static function getDefaultInstance(): SerializationWriterFactoryRegistry {
        if (is_null(self::$defaultInstance)) {
            self::$defaultInstance = new self();
        }
        return self::$defaultInstance;
    }
}