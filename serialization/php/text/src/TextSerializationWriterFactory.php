<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Serialization\Text;


use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactory;

/**
 * Class TextSerializationWriterFactory
 * @package Microsoft\Kiota\Serialization\Text
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 */
class TextSerializationWriterFactory implements SerializationWriterFactory
{

    /**
     * @inheritDoc
     */
    public function getSerializationWriter(string $contentType): SerializationWriter
    {
        if (strtolower($contentType) !== strtolower($this->getValidContentType())) {
            throw new \InvalidArgumentException("Expected content type to be {$this->getValidContentType()}");
        }
        return new TextSerializationWriter();
    }

    /**
     * @inheritDoc
     */
    public function getValidContentType(): string
    {
        return 'text/plain';
    }
}
