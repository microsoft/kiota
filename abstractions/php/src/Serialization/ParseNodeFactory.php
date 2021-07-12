<?php

namespace Microsoft\Kiota\Abstractions\Serialization;

use Psr\Http\Message\StreamInterface;

interface ParseNodeFactory {
    public function getRootParseNode(string $contentType, StreamInterface $rawResponse): ParseNode;
}
