<?php

namespace Microsoft\Kiota\Serialization\Tests;

use GuzzleHttp\Psr7\Utils;
use InvalidArgumentException;
use Microsoft\Kiota\Abstractions\Serialization\ParseNodeFactory;
use Microsoft\Kiota\Serialization\Json\JsonParseNode;
use Microsoft\Kiota\Serialization\Json\JsonParseNodeFactory;
use PHPUnit\Framework\TestCase;
use RuntimeException;

class JsonParseNodeFactoryTest extends TestCase
{
    private ParseNodeFactory $factory;

    public function testGetRootParseNode(): void {
        $this->factory = new JsonParseNodeFactory();
        $root = $this->factory->getRootParseNode('application/json', Utils::streamFor('{}'));
        $this->assertInstanceOf(JsonParseNode::class, $root);
    }

    public function testGetValidContentType(): void{
        $this->factory = new JsonParseNodeFactory();
        $contentType = $this->factory->getValidContentType();
        $this->assertEquals("application/json", $contentType);
    }

    public function testGetRootParseNodeEmptyContentType(): void {
        $this->expectException(InvalidArgumentException::class);
        $this->factory = new JsonParseNodeFactory();
        $this->factory->getRootParseNode('', Utils::streamFor('{}'));
    }

    public function testGetRootParseNodeUnmatchedContentType(): void {
        $this->expectException(InvalidArgumentException::class);
        $this->factory = new JsonParseNodeFactory();
        $this->factory->getRootParseNode('application/xml', Utils::streamFor('{}'));
    }

    public function testGetRootParseNodeInvalidJson(): void {
        $this->expectException(RuntimeException::class);
        $this->factory = new JsonParseNodeFactory();
        $this->factory->getRootParseNode('application/json', Utils::streamFor('This is not json'));
    }
    public function testGetRootParseNodeInvalidJsonEmpty(): void {
        $this->expectException(InvalidArgumentException::class);
        $this->factory = new JsonParseNodeFactory();
        $this->factory->getRootParseNode('application/json', Utils::streamFor(''));
    }
}
