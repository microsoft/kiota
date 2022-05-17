<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Serialization\Text;


use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Serialization\ParseNodeFactory;
use Psr\Http\Message\StreamInterface;

class TextParseNodeFactory implements ParseNodeFactory
{

    /**
     * @inheritDoc
     */
    public function getRootParseNode(string $contentType, StreamInterface $rawResponse): ParseNode
    {
        if (strtolower($contentType) !== strtolower($this->getValidContentType())) {
            throw new \InvalidArgumentException("Expected content type to be {$this->getValidContentType()}");
        }
        $responseContent = $rawResponse->getContents();
        if (!$responseContent) {
            throw new \RuntimeException('Expected a non-empty response stream');
        }
        return new TextParseNode($responseContent);
    }

    /**
     * @inheritDoc
     */
    public function getValidContentType(): string
    {
        return 'text/plain';
    }
}
