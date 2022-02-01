<?php

namespace Microsoft\Kiota\Abstractions\Tests\Types;

use Microsoft\Kiota\Abstractions\Types\Date;
use PHPUnit\Framework\TestCase;

class DateTest extends TestCase
{
    public function testConstructWithDateString(): void
    {
        $dateString = "2022-01-27";
        $date = new Date($dateString);
        $this->assertInstanceOf(Date::class, $date);
        $this->assertEquals($dateString, strval($date));
    }

    public function testConstructorWithInvalidDateStringThrowsException(): void
    {
        $this->expectException(\Exception::class);
        $date = new Date("invalid string");
    }

    public function testCreateFromDateTime(): void
    {
        $date = Date::createFromDateTime(new \DateTime("2022-01-2700:00:00"));
        $this->assertEquals("2022-01-27", strval($date));
    }

    public function testCreateFrom(): void
    {
        $date = Date::createFrom(2022, 01, 27);
        $this->assertEquals("2022-01-27", strval($date));
    }
}
