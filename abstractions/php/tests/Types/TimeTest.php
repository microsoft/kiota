<?php

namespace Microsoft\Kiota\Abstractions\Tests\Types;

use Microsoft\Kiota\Abstractions\Types\Time;
use PHPUnit\Framework\TestCase;

class TimeTest extends TestCase
{
    public function testConstructorWithValidTimeString(): void
    {
        $time = new Time("19:25:00");
        $this->assertInstanceOf(Time::class, $time);
    }

    public function testConstructorWithInvalidTimeString(): void
    {
        $this->expectException(\Exception::class);
        $time = new Time("invalid string");
    }

    public function testCreateFromDateTime(): void
    {
        $time = Time::createFromDateTime(new \DateTime("2022-01-2719:25:00"));
        $this->assertInstanceOf(Time::class, $time);
        $this->assertEquals("19:25:00", strval($time));
    }

    public function testCreateFrom(): void
    {
        $time = Time::createFrom(19, 25, 0);
        $this->assertInstanceOf(Time::class, $time);
        $this->assertEquals("19:25:00", strval($time));
    }
}
