<?php

namespace Microsoft\Kiota\Serialization\Text\Tests;

use GuzzleHttp\Psr7\Utils;
use Microsoft\Kiota\Abstractions\Types\Date;
use Microsoft\Kiota\Abstractions\Types\Time;
use Microsoft\Kiota\Serialization\Text\TextSerializationWriter;
use PHPUnit\Framework\TestCase;

class TextSerializationWriterTest extends TestCase
{
    private TextSerializationWriter $serializationWriter;

    protected function setUp(): void
    {
        $this->serializationWriter = new TextSerializationWriter();
    }

    function testExceptionThrownOnMultipleWrites()
    {
        $this->expectException(\RuntimeException::class);
        $this->serializationWriter->writeIntegerValue(null, 10);
        $this->serializationWriter->writeBooleanValue(null, true);
    }

    function testWriteStringWithNullWritesEmptyString()
    {
        $this->serializationWriter->writeStringValue('', null);
        $this->assertEmpty($this->serializationWriter->getSerializedContent()->getContents());
    }

    function testWriteWithKeyThrowsException()
    {
        $this->expectException(\InvalidArgumentException::class);
        $this->serializationWriter->writeStringValue('key', 'val');
    }

    /**
     * @dataProvider validMethodNamesDataProvider
     */
    function testValidMethods(string $methodName, $value, string $expected)
    {
        call_user_func([$this->serializationWriter, $methodName], null, $value);
        $this->assertEquals($expected, $this->serializationWriter->getSerializedContent()->getContents());
    }

    public function validMethodNamesDataProvider(): array
    {
        return [
            ['writeBooleanValue', false, 'false'],
            ['writeFloatValue', 1.23, '1.23'],
            ['writeIntegerValue', 123, '123'],
            ['writeDateTimeValue', new \DateTime('2022-05-05 22:00:00'), '2022-05-05T22:00:00+00:00'],
            ['writeEnumValue', new TestEnum('valueA'), TestEnum::VALUE_A],
            ['writeDateValue', new Date('2022-05-05'), '2022-05-05'],
            ['writeTimeValue', new Time('22:00:00'), '22:00:00'],
            ['writeDateIntervalValue', new \DateInterval('P1DT22H22M00S'), 'P0Y0M1DT22H22M0S'],
            ['writeBinaryContent', Utils::streamFor('content'), 'content']
        ];
    }

}
