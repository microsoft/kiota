<?php

namespace Microsoft\Kiota\Serialization\Text\Tests;

use Microsoft\Kiota\Abstractions\Enum;
use Microsoft\Kiota\Abstractions\Types\Byte;
use Microsoft\Kiota\Abstractions\Types\Date;
use Microsoft\Kiota\Abstractions\Types\Time;
use Microsoft\Kiota\Serialization\Text\TextParseNode;
use PHPUnit\Framework\TestCase;

class TextParseNodeTest extends TestCase
{
    private TextParseNode $parseNode;
    private string $testResponseContent = 'content';

    protected function setUp(): void
    {
        $this->parseNode = new TextParseNode($this->testResponseContent);
    }

    function testConstructorThrowsExceptionOnEmptyContent()
    {
        $this->expectException(\InvalidArgumentException::class);
        $parseNode = new TextParseNode('');
    }

    /**
     * @dataProvider invalidMethodNamesProvider
     */
    function testInvalidMethodsThrowException(string $methodName)
    {
        $this->expectException(\Exception::class);
        call_user_func([$this->parseNode, $methodName], '');
    }

    public function invalidMethodNamesProvider(): array
    {
        return [
            ['getChildNode'],
            ['getObjectValue'],
            ['getCollectionOfObjectValues'],
            ['getCollectionOfPrimitiveValues']
        ];
    }

    function testValidMethods()
    {
        $this->assertEquals('content', (new TextParseNode('content'))->getStringValue());
        $this->assertTrue((new TextParseNode('true'))->getBooleanValue());
        $this->assertEquals(123, (new TextParseNode('123'))->getIntegerValue());
        $this->assertEquals(1.23, (new TextParseNode('1.23'))->getFloatValue());
        $this->assertInstanceOf(\DateTime::class, (new TextParseNode('2022-05-05 13:56'))->getDateTimeValue());
        $this->assertInstanceOf(\DateInterval::class, (new TextParseNode('P1DT0H0M0S'))->getDateIntervalValue());
        $this->assertInstanceOf(Date::class, (new TextParseNode('2022-05-05'))->getDateValue());
        $this->assertInstanceOf(Time::class, (new TextParseNode('10:00:01'))->getTimeValue());
        $this->assertInstanceOf(TestEnum::class, (new TextParseNode('valueA'))->getEnumValue(TestEnum::class));
        $binaryContent = (new TextParseNode('content'))->getBinaryContent();
        $this->assertEquals('content', $binaryContent->getContents());
        $this->assertInstanceOf(Byte::class, (new TextParseNode('235'))->getByteValue());
    }

}
