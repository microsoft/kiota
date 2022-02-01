<?php

namespace Microsoft\Kiota\Http\Test;

use GuzzleHttp\Client;
use GuzzleHttp\Handler\MockHandler;
use GuzzleHttp\Psr7\Response;
use GuzzleHttp\Psr7\Utils;
use Http\Promise\FulfilledPromise;
use Microsoft\Kiota\Abstractions\Authentication\AuthenticationProvider;
use Microsoft\Kiota\Abstractions\RequestInformation;
use Microsoft\Kiota\Abstractions\ResponseHandler;
use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Serialization\ParseNodeFactory;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactory;
use Microsoft\Kiota\Http\GuzzleRequestAdapter;
use PHPUnit\Framework\TestCase;

class GuzzleRequestAdapterTest extends TestCase
{
    private ParseNode $parseNode;
    private ParseNodeFactory $parseNodeFactory;
    private AuthenticationProvider $authenticationProvider;
    private RequestInformation $requestInformation;
    private string $baseUrl = "https:/graph.microsoft.com";

    protected function setUp(): void
    {
        $this->requestInformation = new RequestInformation();
        $this->requestInformation->httpMethod = 'GET';
        $this->requestInformation->headers = [
            'RequestId' => '1'
        ];
        $this->requestInformation->content = Utils::streamFor('body');
        $this->requestInformation->setUri($this->baseUrl);

        $this->mockParseNode();
        $this->mockParseNodeFactory();
        $this->mockAuthenticationProvider();

        parent::setUp();
    }

    private function mockParseNode(): void
    {
        $this->parseNode = $this->createStub(ParseNode::class);
        $this->parseNode->method('getObjectValue')
                        ->willReturn(new TestUser(1));
        $this->parseNode->method('getCollectionOfObjectValues')
                        ->willReturn([new TestUser(1), new TestUser(2)]);
        $this->parseNode->method('getIntegerValue')
                        ->willReturn(1);
        $this->parseNode->method('getCollectionOfPrimitiveValues')
                        ->willReturn(['a', 'b', 'c']);
    }

    private function mockParseNodeFactory(): void
    {
        $this->parseNodeFactory = $this->createStub(ParseNodeFactory::class);
        $this->parseNodeFactory->method('getRootParseNode')
                                ->willReturn($this->parseNode);
    }

    private function mockAuthenticationProvider(): void
    {
        $this->authenticationProvider = $this->createStub(AuthenticationProvider::class);
        $this->authenticationProvider->method('authenticateRequest')
                                    ->willReturn(new FulfilledPromise(null));
    }

    private function mockRequestAdapter(array $mockResponses = []): GuzzleRequestAdapter
    {
        $requestAdapter = new GuzzleRequestAdapter(
            $this->authenticationProvider,
            $this->parseNodeFactory,
            $this->createMock(SerializationWriterFactory::class),
            new Client(['handler' => new MockHandler($mockResponses)])
        );
        $requestAdapter->setBaseUrl($this->baseUrl);
        return $requestAdapter;
    }

    public function testGetPsrRequestFromRequestInformation(): void
    {
        $psrRequest = $this->mockRequestAdapter()->getPsrRequestFromRequestInformation($this->requestInformation);
        $this->assertEquals($this->requestInformation->httpMethod, $psrRequest->getMethod());
        $this->assertEquals($this->requestInformation->headers['RequestId'], $psrRequest->getHeaderLine('RequestId'));
        $this->assertEquals('body', $psrRequest->getBody()->getContents());
        $this->assertEquals($this->requestInformation->getUri(), strval($psrRequest->getUri()));
    }

    public function testSendAsync(): void
    {
        $requestAdapter = $this->mockRequestAdapter([new Response(200, ['Content-Type' => 'application/json'])]);
        $promise = $requestAdapter->sendAsync($this->requestInformation, TestUser::class);
        $this->assertInstanceOf(TestUser::class, $promise->wait());
    }

    public function testSendAsyncWithResponseHandler(): void
    {
        $requestAdapter = $this->mockRequestAdapter([new Response(200, ['Content-Type' => 'application/json'])]);
        $customResponseHandler = $this->createMock(ResponseHandler::class);
        $customResponseHandler->expects($this->once())
            ->method('handleResponseAsync');
        $requestAdapter->sendAsync($this->requestInformation, TestUser::class, $customResponseHandler);
    }

    public function testSendCollectionAsync(): void
    {
        $requestAdapter = $this->mockRequestAdapter([new Response(200, ['Content-Type' => 'application/json'])]);
        $promise = $requestAdapter->sendCollectionAsync($this->requestInformation, TestUser::class);
        $result = $promise->wait();
        $this->assertIsArray($result);
        $this->assertTrue(sizeof($result) == 2);
        $this->assertInstanceOf(TestUser::class, $result[0]);
    }

    public function testSendCollectionAsyncWithResponseHandler(): void
    {
        $requestAdapter = $this->mockRequestAdapter([new Response(200, ['Content-Type' => 'application/json'])]);
        $customResponseHandler = $this->createMock(ResponseHandler::class);
        $customResponseHandler->expects($this->once())
            ->method('handleResponseAsync');
        $requestAdapter->sendCollectionAsync($this->requestInformation, TestUser::class, $customResponseHandler);
    }

    public function testSendPrimitiveAsync(): void
    {
        $requestAdapter = $this->mockRequestAdapter([new Response(200, ['Content-Type' => 'application/json'])]);
        $promise = $requestAdapter->sendPrimitiveAsync($this->requestInformation, 'int');
        $this->assertEquals(1, $promise->wait());
    }

    public function testSendPrimitiveAsyncThrowsExceptionForUnsupportedType(): void
    {
        $this->expectException(\InvalidArgumentException::class);
        $requestAdapter = $this->mockRequestAdapter([new Response(200, ['Content-Type' => 'application/json'])]);
        $promise = $requestAdapter->sendPrimitiveAsync($this->requestInformation, 'callable');
        $promise->wait();
    }

    public function testSendPrimitiveAsyncWithResponseHandler(): void
    {
        $requestAdapter = $this->mockRequestAdapter([new Response(200, ['Content-Type' => 'application/json'])]);
        $customResponseHandler = $this->createMock(ResponseHandler::class);
        $customResponseHandler->expects($this->once())
            ->method('handleResponseAsync');
        $requestAdapter->sendPrimitiveAsync($this->requestInformation, 'int', $customResponseHandler);
    }

    public function testSendPrimitiveCollectionAsync(): void
    {
        $requestAdapter = $this->mockRequestAdapter([new Response(200, ['Content-Type' => 'application/json'])]);
        $promise = $requestAdapter->sendPrimitiveCollectionAsync($this->requestInformation, 'string');
        $result = $promise->wait();
        $this->assertIsArray($result);
        $this->assertTrue(sizeof($result) == 3);
        $this->assertTrue(is_string($result[0]));
    }

    public function testSendPrimitiveCollectionAsyncWithResponseHandler(): void
    {
        $requestAdapter = $this->mockRequestAdapter([new Response(200, ['Content-Type' => 'application/json'])]);
        $customResponseHandler = $this->createMock(ResponseHandler::class);
        $customResponseHandler->expects($this->once())
            ->method('handleResponseAsync');
        $requestAdapter->sendPrimitiveCollectionAsync($this->requestInformation, 'string', $customResponseHandler);
    }

    public function testSendNoContentAsync(): void
    {
        $requestAdapter = $this->mockRequestAdapter([new Response(200, ['Content-Type' => 'application/json'])]);
        $promise = $requestAdapter->sendNoContentAsync($this->requestInformation);
        $this->assertNull($promise->wait());
    }

    public function testSendNoContentAsyncWithResponseHandler(): void
    {
        $requestAdapter = $this->mockRequestAdapter([new Response(200, ['Content-Type' => 'application/json'])]);
        $customResponseHandler = $this->createMock(ResponseHandler::class);
        $customResponseHandler->expects($this->once())
            ->method('handleResponseAsync');
        $requestAdapter->sendNoContentAsync($this->requestInformation,  $customResponseHandler);
    }

}

class TestUser implements Parsable {
    /**
     * @var array<string, mixed> $additionalData
     */
    private array $additionalData = [];
    private ?int $id;

    public function __construct(?int $id = null)
    {
        $this->id = $id;
    }

    /**
     * @return int|null
     */
    public function getId(): ?int
    {
        return $this->id;
    }

    public function getFieldDeserializers(): array
    {
        return [
            "id" => function (self $o, ParseNode $n) {$o->setId($n->getIntegerValue());}
        ];
    }

    public function setId(?int $value): void {
        $this->id = $value;
    }

    public function serialize(SerializationWriter $writer): void
    {
        $writer->writeIntegerValue('id', $this->id);
    }

    public function getAdditionalData(): ?array {
        return $this->additionalData;
    }

    public function setAdditionalData(array $value): void
    {
        $this->additionalData = $value;
    }
}
