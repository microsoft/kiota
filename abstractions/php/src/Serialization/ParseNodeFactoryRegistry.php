<?php
namespace Microsoft\Kiota\Abstractions\Serialization;


use Psr\Http\Message\StreamInterface;

class ParseNodeFactoryRegistry implements ParseNodeFactoryInterface {

    /**
     * @var array<string, ParseNodeFactoryInterface>
     */

    public array $contentTypeAssociatedFactories = [];

    public function getParseNode(string $contentType, StreamInterface $rawResponse): AbstractParseNode {
        if (array_key_exists($contentType, $this->contentTypeAssociatedFactories)) {
            return $this->contentTypeAssociatedFactories[$contentType]->getParseNode($contentType, $rawResponse);
        }
        throw new \UnexpectedValueException('Content type ' . $contentType . ' does not have a factory to be parsed');
    }
}
