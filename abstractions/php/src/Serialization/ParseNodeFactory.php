<?php

namespace Microsoft\Kiota\Abstractions\Serialization;

use Psr\Http\Message\StreamInterface;

interface ParseNodeFactory {
    public function getParseNode(?string $contentType, ?StreamInterface $rawResponse): ?ParseNode;
}
