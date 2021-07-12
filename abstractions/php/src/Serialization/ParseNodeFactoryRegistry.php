<?php
namespace Microsoft\Kiota\Abstractions\Serialization;


use Psr\Http\Message\StreamInterface;

class ParseNodeFactoryRegistry implements ParseNodeFactory {

    /**
     * @var array<string, ParseNodeFactory>
     */

    public array $contentTypeAssociatedFactories = [];

    public function getRootParseNode(string $contentType, StreamInterface $rawResponse): ParseNode {
        if (array_key_exists($contentType, $this->contentTypeAssociatedFactories)) {
            return $this->contentTypeAssociatedFactories[$contentType]->getRootParseNode($contentType, $rawResponse);
        }
        throw new \UnexpectedValueException('Content type ' . $contentType . ' does not have a factory to be parsed');
    }
}
