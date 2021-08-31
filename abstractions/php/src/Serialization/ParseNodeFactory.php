<?php

namespace Microsoft\Kiota\Abstractions\Serialization;

use Psr\Http\Message\StreamInterface;

interface ParseNodeFactory {

    /**
     * @param string $contentType
     * @param StreamInterface $rawResponse
     * @return ParseNode
     */
    public function getParseNode(string $contentType, StreamInterface $rawResponse): ParseNode;
    public function getValidContentType(): string;
}
