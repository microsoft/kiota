<?php

namespace Microsoft\Kiota\Http\Test;

use GuzzleHttp\HandlerStack;
use Microsoft\Kiota\Http\KiotaClientFactory;
use PHPUnit\Framework\TestCase;

class KiotaClientFactoryTest extends TestCase
{
    public function testCreateReturnsClient()
    {
        $this->assertInstanceOf(\GuzzleHttp\Client::class, KiotaClientFactory::create());
    }

    public function testCreateWithMiddleware()
    {
        $handlerStack = new HandlerStack();
        $this->assertInstanceOf(\GuzzleHttp\Client::class, KiotaClientFactory::createWithMiddleware($handlerStack));
    }

    public function testGetDefaultHandlerStack()
    {
        $this->assertInstanceOf(HandlerStack::class, KiotaClientFactory::getDefaultHandlerStack());
    }
}
