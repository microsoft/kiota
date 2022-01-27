<?php

namespace Microsoft\Kiota\Serialization\Tests;

use DateTime;
use DateTimeInterface;
use Exception;
use GuzzleHttp\Psr7\Utils;
use Microsoft\Kiota\Abstractions\Enum;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Types\Byte;
use Microsoft\Kiota\Abstractions\Types\Date;
use Microsoft\Kiota\Abstractions\Types\Time;
use Microsoft\Kiota\Serialization\Json\JsonParseNode;
use Microsoft\Kiota\Serialization\Json\JsonParseNodeFactory;
use Microsoft\Kiota\Serialization\Tests\Samples\MaritalStatus;
use Microsoft\Kiota\Serialization\Tests\Samples\Person;
use PHPUnit\Framework\TestCase;
use Psr\Http\Message\StreamInterface;

class JsonParseNodeTest extends TestCase
{
    private ParseNode $parseNode;
    private StreamInterface $stream;

    protected function setUp(): void {
        $this->stream = Utils::streamFor('{"name": "Silas Kenneth", "age": 98, "height": 123.122, "maritalStatus": "complicated,single"}');
    }

    public function testGetIntegerValue(): void {
        $this->parseNode = new JsonParseNode('1243.78');
        $expected = $this->parseNode->getIntegerValue();
        $this->assertEquals(1243, $expected);
    }

    public function testGetCollectionOfObjectValues(): void {
        $str = Utils::streamFor('[{"name": "Silas Kenneth", "age": 98, "height": 123.122, "maritalStatus": "complicated,single"},{"name": "James Bay", "age": 23, "height": 163.122, "maritalStatus": "married"}]');
        $this->parseNode = (new JsonParseNodeFactory())->getRootParseNode('application/json', $str);

        /** @var array<Person> $expected */
        $expected = $this->parseNode->getCollectionOfObjectValues(Person::class);
        $this->assertCount(2, $expected);
        $this->assertInstanceOf(Person::class, $expected[1]);
        $this->assertInstanceOf(Person::class, $expected[0]);
        $this->assertEquals('James Bay', $expected[1]->getName());
    }

    /**
     * @throws \Exception
     */
    public function testGetObjectValue(): void {
        $this->parseNode = (new JsonParseNodeFactory())->getRootParseNode('application/json', $this->stream);
        /** @var Person $expected */
        $expected = $this->parseNode->getObjectValue(Person::class);
        $this->assertInstanceOf(Person::class, $expected);
        $this->assertEquals('Silas Kenneth', $expected->getName());
        $this->assertInstanceOf(Enum::class, $expected->getMaritalStatus());
        $this->assertEquals(98, $expected->getAge());
        $this->assertEquals(123.122, $expected->getHeight());
    }

    public function testGetChildNode(): void {

    }

    public function testGetFloatValue(): void {
        $this->parseNode = new JsonParseNode('1243.12');
        $expected = $this->parseNode->getFloatValue();
        $this->assertEquals(1243.12, $expected);
    }

    public function testGetCollectionOfPrimitiveValues(): void {
        $this->parseNode = new JsonParseNode([1921, 1212,123,45,56]);
        $expected = $this->parseNode->getCollectionOfPrimitiveValues();
        $this->assertEquals([], $expected);
    }

    public function testGetAnyValue(): void {

    }

    public function testGetByteValue(): void {
        $this->parseNode = new JsonParseNode('23');
        $expected = $this->parseNode->getByteValue();
        $this->assertInstanceOf(Byte::class, $expected);
        $this->assertEquals(23, (string)$expected);
        $this->expectException(Exception::class);
        $this->parseNode = new JsonParseNode('-192');
        $this->parseNode->getByteValue();
    }

    public function testGetEnumValue(): void {
        $this->parseNode = new JsonParseNode('married');
        /** @var Enum $expected */
        $expected = $this->parseNode->getEnumValue(MaritalStatus::class);
        $this->assertInstanceOf(Enum::class, $expected);
        $this->assertEquals('married', $expected->value());
        $this->parseNode = new JsonParseNode('married,single');
        /** @var Enum $expected */
        $expected = $this->parseNode->getEnumValue(MaritalStatus::class);
        $this->assertInstanceOf(Enum::class, $expected);
        $this->assertEquals('married,single', $expected->value());
    }

    /**
     * @throws \Exception
     */
    public function testGetTimeOnlyValue(): void{
        $this->parseNode = new JsonParseNode((new DateTime('2022-01-27T12:59:45.596117'))->format(DATE_ATOM));
        $expected = $this->parseNode->getTimeOnlyValue();
        $this->assertInstanceOf(Time::class, $expected);
        $this->assertEquals('12:59:45', (string)$expected);
    }

    /**
     * @throws \Exception
     */
    public function testGetDateOnlyValue(): void{
        $this->parseNode = new JsonParseNode((new DateTime('2022-01-27T12:59:45.596117'))->format(DATE_ATOM));
        $expected = $this->parseNode->getDateOnlyValue();
        $this->assertInstanceOf(Date::class, $expected);
        $this->assertEquals('2022-01-27', (string)$expected);
    }

    public function testGetBooleanValue(): void {
        $this->parseNode = new JsonParseNode(true);
        $expected = $this->parseNode->getBooleanValue();
        $this->assertEquals('bool', get_debug_type($expected));
        $this->assertEquals(true, $expected);
    }

    /**
     * @throws \Exception
     */
    public function testGetDateTimeOffsetValue(): void {
        $value = (new DateTime('2022-01-27T12:59:45.596117'))->format(DateTimeInterface::RFC3339);
        $this->parseNode = new JsonParseNode($value);
        $expected = $this->parseNode->getDateTimeOffsetValue();
        $this->assertInstanceOf(DateTime::class, $expected);
        $this->assertEquals($value, $expected->format(DateTimeInterface::RFC3339));
    }

    public function testGetStringValue(): void{
        $this->parseNode = new JsonParseNode('Silas Kenneth was here');
        $expected = $this->parseNode->getStringValue();
        $this->assertEquals('Silas Kenneth was here', $expected);
    }

    public function testGetUUIDValue(): void{
        $this->parseNode = new JsonParseNode('4cbfc23b-d081-4bd2-90f5-61deebfa755c');
        $expected = $this->parseNode->getStringValue();
        $this->assertEquals('4cbfc23b-d081-4bd2-90f5-61deebfa755c', $expected);
    }

    public function testGetLongValue(): void {
        $this->parseNode = new JsonParseNode(6892759229690544128);
        $expected = $this->parseNode->getLongValue();
        $this->assertEquals(6892759229690544128, $expected);
    }
}
