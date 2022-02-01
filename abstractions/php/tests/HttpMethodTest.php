<?php

namespace Microsoft\Kiota\Abstractions\Tests;

use InvalidArgumentException;
use Microsoft\Kiota\Abstractions\HttpMethod;
use PHPUnit\Framework\TestCase;

class HttpMethodTest extends TestCase {
    public function testCanOnlyBeValidHttpMethod(): void {
        $this->expectException(InvalidArgumentException::class);
        new HttpMethod('RANDOM');
    }

    public function testCanSetValidHttpMethod(): void {
        $httpMethod = new HttpMethod(HttpMethod::GET);
        $this->assertEquals('GET', $httpMethod->value());
    }
}