<?php

namespace Microsoft\Kiota\Serialization\Tests;

use GuzzleHttp\Psr7\Utils;
use Microsoft\Kiota\Serialization\Json\JsonParseNodeFactory;
use Microsoft\Kiota\Serialization\Json\JsonSerializationWriter;
use Microsoft\Kiota\Serialization\Tests\Samples\Message;
use PHPUnit\Framework\TestCase;

class JsonParseNodeFactoryTest extends TestCase
{
    private JsonParseNodeFactory $factory;


    protected function setUp(): void {
        $this->factory = new JsonParseNodeFactory();
    }

    /**
     * @throws \JsonException
     * @throws \Exception
     */
    public function testGetParseNode(): void {
        $file = fopen(__DIR__.'./Samples/message.json', 'rb+');
        $doc = Utils::streamFor($file);
        $jsonParseNode = (new JsonParseNodeFactory())->getRootParseNode('application/json', $doc);
        $ff = $jsonParseNode->getObjectValue(Message::class);
        $serializer = new JsonSerializationWriter();
        $ff->serialize($serializer);

//        print_r(json_decode($doc, true, 512, JSON_THROW_ON_ERROR));
        echo "\n\n";
        echo "\n\n";
        $cont = '{'.$serializer->getSerializedContent().'}';

        echo $cont;

        print_r($ff);

        print_r(json_decode($cont, true, 512, JSON_THROW_ON_ERROR));

    }

    public function testGetValidContentType(): void {

    }
}
