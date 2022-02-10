<?php
namespace Microsoft\Kiota\Abstractions\Serialization;


use InvalidArgumentException;
use Psr\Http\Message\StreamInterface;
use RuntimeException;
use UnexpectedValueException;


/**
 * This factory holds a list of all the registered factories for the various types of nodes.
 */
class ParseNodeFactoryRegistry implements ParseNodeFactory {

    /**
     * Default singleton instance of the registry to be used when registring new factories that should be available by default.
     * @var ParseNodeFactoryRegistry|null
     */
    private static ?ParseNodeFactoryRegistry $defaultInstance = null;

    /**
     *  List of factories that are registered by content type.
     * @var array<string, ParseNodeFactory>
     */
    public array $contentTypeAssociatedFactories = [];

    /**
     * @param string $contentType
     * @param StreamInterface $rawResponse
     * @return ParseNode
     */
    public function getRootParseNode(string $contentType, StreamInterface $rawResponse): ParseNode {
        if (empty(trim($contentType))) {
            throw new InvalidArgumentException('$contentType cannot be empty.');
        }
        if (array_key_exists($contentType, $this->contentTypeAssociatedFactories)) {
            return $this->contentTypeAssociatedFactories[$contentType]->getRootParseNode($contentType, $rawResponse);
        }
        throw new UnexpectedValueException('Content type ' . $contentType . ' does not have a factory to be parsed');
    }

    public static function getDefaultInstance(): ParseNodeFactoryRegistry {
        if (is_null(self::$defaultInstance)) {
            self::$defaultInstance = new self();
        }
        return self::$defaultInstance;
    }

    public function getValidContentType(): string {
        throw new RuntimeException('The registry supports multiple content types. Get the registered factory instead.');
    }
}
