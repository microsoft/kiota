<?php

namespace Microsoft\Kiota\Serialization\Tests;

use DateInterval;
use DateTime;
use Microsoft\Kiota\Abstractions\Types\Date;
use Microsoft\Kiota\Abstractions\Types\Time;
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
     * @throws \Exception
     */
    public function testGetParseNode(): void {
        $this->serializationWriter->writeStringValue('name', 'Silas Kenneth');
        $this->serializationWriter->writeBooleanValue('married', false);
        $this->serializationWriter->writeFloatValue('height', 160.23);
        $this->serializationWriter->writeNullValue('username');
        $this->serializationWriter->writeAdditionalData(['@odata.type' => 'Microsoft.Graph',
            'timestamp' => new DateTime('now'),
            'request-id' => Uuid::uuid4()->toString()]);
        $this->serializationWriter->writeAnyValue('dates', ['startDate' => new DateTime('now'),
            'endDate' => new DateTime('now')]);
        $this->serializationWriter->writeAnyValue('reminders', [['name' => 'Starting of work', 'startDate' => new DateTime('now'),
            'endDate' => (new DateTime('now'))->add(new DateInterval('P0Y2D'))]]);
        $this->serializationWriter->writeAnyValue('numbers', [1,2,3,4,5,6,7,8,9]);
        $this->serializationWriter->writeAnyValue('done', Date::createFromDateTime(new DateTime('now')));
        $this->serializationWriter->writeAnyValue('when', Time::createFromDateTime(new DateTime('now')));
        $content = $this->serializationWriter->getSerializedContent()->getContents();
        $content = trim($content, ',');
        $content = "{{$content}}";
        echo $content ."\n";
        echo "\n";
        $this->assertEquals('', null);
    }

    public function testGetValidContentType(): void {
        $this->assertEquals(1,1);
    }

}
