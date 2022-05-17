<?php

namespace Microsoft\Kiota\Serialization\Text\Tests;

use GuzzleHttp\Psr7\Utils;
use Microsoft\Kiota\Serialization\Text\TextParseNodeFactory;
use PHPUnit\Framework\TestCase;

class TextParseNodeFactoryTest extends TestCase
{
    private TextParseNodeFactory $parseNodeFactory;

    protected function setUp(): void
    {
        $this->parseNodeFactory = new TextParseNodeFactory();
    }

    function testGetRootParseNodeThrowsExceptionOnInvalidContentType()
    {
        $this->expectException(\InvalidArgumentException::class);
        $this->parseNodeFactory->getRootParseNode('text/html', Utils::streamFor('contents'));
    }

    function testGetRootParseNodeThrowsExceptionOnEmptyResponseStream()
    {
        $this->expectException(\RuntimeException::class);
        $this->parseNodeFactory->getRootParseNode('text/plain', Utils::streamFor());
    }
}
