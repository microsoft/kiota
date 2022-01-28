<?php

namespace Microsoft\Kiota\Serialization\Tests;

use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;
use Microsoft\Kiota\Abstractions\Types\Byte;
use Microsoft\Kiota\Abstractions\Types\Date;
use Microsoft\Kiota\Abstractions\Types\Time;
use Microsoft\Kiota\Serialization\Json\JsonSerializationWriter;
use Microsoft\Kiota\Serialization\Tests\Samples\Address;
use Microsoft\Kiota\Serialization\Tests\Samples\MaritalStatus;
use Microsoft\Kiota\Serialization\Tests\Samples\Person;
use PHPUnit\Framework\TestCase;

class JsonSerializationWriterTest extends TestCase
{
    private SerializationWriter $jsonSerializationWriter;

    /**
     */
    public function testWriteAdditionalData(): void {
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeAdditionalData(['@odata.type' => 'Type']);
        $expected = '"@odata.type":"Type"';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    public function testWriteLongValue(): void {
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeLongValue("timestamp", 28192199291929192);
        $expected = '"timestamp":28192199291929192';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    /**
     * @throws \Exception
     */
    public function testWriteDateOnlyValue(): void {
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $date = Date::createFrom(2012, 12, 3);
        $this->jsonSerializationWriter->writeDateOnlyValue("date", $date);
        $expected = '"date":"2012-12-03"';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    public function testWriteUUIDValue(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeUUIDValue("id", '9de7828f-4975-49c7-8734-805487dfb8a2');
        $expected = '"id":"9de7828f-4975-49c7-8734-805487dfb8a2"';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    /**
     */
    public function testWriteCollectionOfNonParsableObjectValues(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeCollectionOfNonParsableObjectValues("stops", [1,2,3,4,5]);
        $expected = '"stops":[1,2,3,4,5]';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeAnyValue("stops", ["first" => 'First', 'second' => 'Second']);
        $expected2 = '"stops":{"first":"First","second":"Second"}';
        $actual2 = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
        $this->assertEquals($expected2, $actual2);
    }

    public function testWriteFloatValue(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeFloatValue("height", 12.394);
        $expected = '"height":12.394';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    /**
     */
    public function testWriteEnumSetValue(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeAnyValue("status", new MaritalStatus('married,complicated'));
        $expected = '"status":"married,complicated"';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    public function testWriteNullValue(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeNullValue("nextPage");
        $expected = '"nextPage":null';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    /**
     */
    public function testWriteCollectionOfObjectValues(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $person1 = new Person();
        $person1->setName("John");
        $person1->setMaritalStatus(new MaritalStatus('single'));
        $person2 = new Person();
        $person2->setName('Jane');
        $person2->setMaritalStatus(new MaritalStatus('married'));
        $this->jsonSerializationWriter->writeAnyValue("to", [$person1, $person2]);
        $expected = '"to":[{"name":"John","age":,"maritalStatus":"single","height":,"address":null},{"name":"Jane","age":,"maritalStatus":"married","height":,"address":null}]';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    public function testWriteObjectValue(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $person1 = new Person();
        $person1->setName("John");
        $person1->setMaritalStatus(new MaritalStatus('single'));
        $address = new Address();
        $address->setCity('Nairobi');
        $person1->setAddress($address);
        $this->jsonSerializationWriter->writeAnyValue("to", $person1);
        $expected = '"to":{"name":"John","age":,"maritalStatus":"single","height":,"address":{"street":null,"city":"Nairobi"}}';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    public function testWriteEnumValue(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeEnumValue("status", new MaritalStatus('married'));
        $expected = '"status":"married"';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    public function testWriteAnyValue(): void {
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $byte = new Byte(23);
        $this->jsonSerializationWriter->writeAnyValue("to", $byte);
        $expected = '"to":23';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    /**
     * @throws \Exception
     */
    public function testWriteNonParsableObjectValue(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeAnyValue("times", [
            "start" => Time::createFrom(12,0, 23),
            "end" => Time::createFrom(13, 45, 12)]);
        $expected = '"times":{"start":"12:00:23","end":"13:45:12"}';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    public function testWriteBooleanValue(): void {
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeAnyValue("available", true);
        $expected = '"available":true';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    /**
     * @throws \Exception
     */
    public function testWriteTimeOnlyValue(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeAnyValue("time", Time::createFromDateTime(new \DateTime('2018-12-12T12:34:42+00:00Z')));
        $expected = '"time":"12:34:42"';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    public function testWriteIntegerValue(): void {
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeAnyValue("age", 23);
        $expected = '"age":23';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    public function testWriteDateTimeOffsetValue(): void {
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeAnyValue("dateTime", new \DateTime('2018-12-12T12:34:42+00:00Z'));
        $expected = '"dateTime":"2018-12-12T12:34:42+00:00Z"';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    public function testGetSerializedContent(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeAnyValue("statement", "This is a string");
        $expected = '"statement":"This is a string"';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    /**
     */
    public function testWriteByteValue(): void{
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $byte = new Byte(23);
        $this->jsonSerializationWriter->writeAnyValue("to", $byte);
        $expected = '"to":23';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }

    /**
     */
    public function testWriteStringValue(): void {
        $this->jsonSerializationWriter = new JsonSerializationWriter();
        $this->jsonSerializationWriter->writeAnyValue("statement", "This is a string");
        $expected = '"statement":"This is a string"';
        $actual = $this->jsonSerializationWriter->getSerializedContent()->getContents();
        $this->assertEquals($expected, $actual);
    }
}
