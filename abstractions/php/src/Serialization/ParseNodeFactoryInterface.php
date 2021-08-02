<?php

namespace Microsoft\Kiota\Abstractions\Serialization;

use Psr\Http\Message\StreamInterface;

interface ParseNodeFactoryInterface {

    /**
     * @param string $contentType
     * @param StreamInterface $rawResponse
     * @return AbstractParseNode
     */
    public function getParseNode(string $contentType, StreamInterface $rawResponse): AbstractParseNode;
    public function getValidContentType(): string;
}
