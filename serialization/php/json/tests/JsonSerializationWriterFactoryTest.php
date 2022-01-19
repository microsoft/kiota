<?php

namespace Microsoft\Kiota\Serialization\Tests;

use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;
use Microsoft\Kiota\Serialization\Json\JsonSerializationWriter;
use PHPUnit\Framework\TestCase;

class JsonSerializationWriterFactoryTest extends TestCase
{
    private SerializationWriter $serializationWriter;

    protected function setUp(): void {
        $this->serializationWriter = new JsonSerializationWriter();
    }

    /**
     * @throws \JsonException
     */
    public function testGetParseNode(): void {
        $this->serializationWriter->writeStringValue('name', 'Silas Kenneth');
        $this->serializationWriter->writeBooleanValue('married', true);
        $this->serializationWriter->writeFloatValue('height', 160.23);
        $this->serializationWriter->writeNullValue('username');
        $content = $this->serializationWriter->getSerializedContent()->getContents();
        $content = trim($content, ',');
        $content = "{{$content}}";
        echo $content ."\n";
        $object = json_decode($content, true, 512, JSON_THROW_ON_ERROR | JSON_BIGINT_AS_STRING);
        print_r(gettype($object));
        $this->assertEquals('', null);
    }

    public function testGetValidContentType(): void {
    }
}
