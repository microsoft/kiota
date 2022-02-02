<?php

namespace Microsoft\Kiota\Abstractions\Serialization;

use Psr\Http\Message\StreamInterface;

/**
 * Defines the contract for a factory that is used to create {@link ParseNode}s.
 */
interface ParseNodeFactory {

    /**
     * Creates a {@link ParseNode} from the given {@link StreamInterface} and content type.
     * @param string $contentType the {@link StreamInterface} to read from.
     * @param StreamInterface $rawResponse the content type of the {@link StreamInterface}.
     * @return ParseNode a {@link ParseNode} that can deserialize the given {@link StreamInterface}.
     */
    public function getRootParseNode(string $contentType, StreamInterface $rawResponse): ParseNode;

    /**
     * Returns the content type this factory's parse nodes can deserialize.
     */
    public function getValidContentType(): string;
}
