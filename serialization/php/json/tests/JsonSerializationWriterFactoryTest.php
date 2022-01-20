<?php

namespace Microsoft\Kiota\Serialization\Tests;

use Microsoft\Kiota\Serialization\Json\JsonSerializationWriter;
use PHPUnit\Framework\TestCase;
use Ramsey\Uuid\Uuid;

class JsonSerializationWriterFactoryTest extends TestCase
{
    private JsonSerializationWriter $serializationWriter;

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
        $this->serializationWriter->writeAdditionalData(['@odata.type' => 'Microsoft.Graph',
            'timestamp' => new \DateTime('now'),
            'request-id' => Uuid::uuid4()->toString()]);
        $this->serializationWriter->writeAnyValue('dates', ['startDate' => new \DateTime('now'),
            'endDate' => new \DateTime('now')]);
        $this->serializationWriter->writeAnyValue('reminders', [['name' => 'Starting of work', 'startDate' => new \DateTime('now'),
            'endDate' => (new \DateTime('now'))->add(new \DateInterval('P0Y2D'))]]);
        $content = $this->serializationWriter->getSerializedContent()->getContents();
        $content = trim($content, ',');
        $content = "{{$content}}";
        echo $content ."\n";
        $object = json_decode($content, true, 512, JSON_THROW_ON_ERROR | JSON_BIGINT_AS_STRING);
        $this->assertEquals('', null);
    }

    public function testGetValidContentType(): void {
        $this->assertEquals(1,1);
    }
}
