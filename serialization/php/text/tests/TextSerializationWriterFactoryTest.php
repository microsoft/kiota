<?php

namespace Microsoft\Kiota\Serialization\Text\Tests;

use Microsoft\Kiota\Serialization\Text\TextSerializationWriter;
use Microsoft\Kiota\Serialization\Text\TextSerializationWriterFactory;
use PHPUnit\Framework\TestCase;

class TextSerializationWriterFactoryTest extends TestCase
{
    function testGetSerializationWriterThrowsExceptionOnInvalidContentType()
    {
        $this->expectException(\Exception::class);
        (new TextSerializationWriterFactory())->getSerializationWriter('');
    }

    function testGetSerializationWriterWithValidContentType()
    {
        $this->assertInstanceOf(TextSerializationWriter::class, (new TextSerializationWriterFactory())->getSerializationWriter('text/plain'));
    }
}
