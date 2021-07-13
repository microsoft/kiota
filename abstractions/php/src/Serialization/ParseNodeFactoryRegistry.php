<?php
namespace Microsoft\Kiota\Abstractions\Serialization;


use Psr\Http\Message\StreamInterface;

class ParseNodeFactoryRegistry implements ParseNodeFactory {

    /**
     * @var array<string, ParseNodeFactory>
     */

    public array $contentTypeAssociatedFactories = [];

    public function getParseNode(?string $contentType, ?StreamInterface $rawResponse): ?ParseNode {
        if (is_null($contentType)) {
            throw new \InvalidArgumentException('parameter contentType cannot be null');
        }
        if (is_null($rawResponse)) {
            throw new \InvalidArgumentException('parameter rawResponse cannot be null');
        }

        if (array_key_exists($contentType, $this->contentTypeAssociatedFactories)) {
            return $this->contentTypeAssociatedFactories[$contentType]->getParseNode($contentType, $rawResponse);
        }
        throw new \UnexpectedValueException('Content type ' . $contentType . ' does not have a factory to be parsed');
    }
}
