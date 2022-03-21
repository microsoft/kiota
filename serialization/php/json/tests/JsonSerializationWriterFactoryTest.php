<?php

namespace Microsoft\Kiota\Serialization\Tests;

use InvalidArgumentException;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactory;
use Microsoft\Kiota\Serialization\Json\JsonSerializationWriter;
use Microsoft\Kiota\Serialization\Json\JsonSerializationWriterFactory;
use PHPUnit\Framework\TestCase;

class JsonSerializationWriterFactoryTest extends TestCase
{
    private SerializationWriterFactory  $factory;

    public function testGetSerializationWriter(): void{
        $this->factory = new JsonSerializationWriterFactory();
        $writer = $this->factory->getSerializationWriter('application/json');
        $this->assertInstanceOf(JsonSerializationWriter::class, $writer);
    }

    public function testGetValidContentType(): void{
        $this->factory = new JsonSerializationWriterFactory();
        $contentType = $this->factory->getValidContentType();
        $this->assertEquals('application/json', $contentType);
    }

    public function testGetSerializationWriterEmptyContentType(): void {
        $this->expectException(InvalidArgumentException::class);
        $this->factory = new JsonSerializationWriterFactory();
        $this->factory->getSerializationWriter('');
    }

    public function testGetSerializationWriterUnmatchedContentType(): void {
        $this->expectException(InvalidArgumentException::class);
        $this->factory = new JsonSerializationWriterFactory();
        $this->factory->getSerializationWriter('application/xml');
    }
}
