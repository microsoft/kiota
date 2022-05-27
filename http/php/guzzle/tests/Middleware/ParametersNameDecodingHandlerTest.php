<?php

namespace Microsoft\Kiota\Http\Test\Middleware;

use GuzzleHttp\Client;
use GuzzleHttp\Handler\MockHandler;
use GuzzleHttp\HandlerStack;
use GuzzleHttp\Psr7\Response;
use Microsoft\Kiota\Http\Middleware\KiotaMiddleware;
use Microsoft\Kiota\Http\Middleware\Options\ParametersDecodingOption;
use PHPUnit\Framework\TestCase;
use Psr\Http\Message\RequestInterface;
use Psr\Http\Message\ResponseInterface;

class ParametersNameDecodingHandlerTest extends TestCase
{
    private string $defaultUrl = 'https://graph.microsoft.com/me/messages?%24top=10&created%2din=2022-10-05&subject%2ename=%7eWelcome';
    private string $defaultDecodedUrl = 'https://graph.microsoft.com/me/messages?$top=10&created-in=2022-10-05&subject.name=~Welcome';

    public function testDefaultDecoding()
    {
        $mockResponse = [
            function (RequestInterface $request, array $options) {
                $this->assertEquals($this->defaultDecodedUrl, strval($request->getUri()));
                return new Response(200);
            }
        ];
        $this->executeMockRequest($mockResponse);
    }

    public function testDisableDecoding()
    {
        $mockResponse = [
            function (RequestInterface $request, array $options) {
                $this->assertEquals($this->defaultUrl, strval($request->getUri()));
                return new Response(200);
            }
        ];
        $decodingOption = new ParametersDecodingOption();
        $decodingOption->setEnabled(false);
        $this->executeMockRequest($mockResponse, $decodingOption);
    }

    public function testCustomCharactersToDecode()
    {
        $url = $this->defaultUrl."&%23someId=1";
        $expectedDecoded = $this->defaultUrl."&#someId=1";

        $mockResponse = [
            function (RequestInterface $request, array $options) use ($expectedDecoded) {
                $this->assertEquals($expectedDecoded, strval($request->getUri()));
                return new Response(200);
            }
        ];
        $decodingOption = new ParametersDecodingOption(['#']);
        $this->executeMockRequest($mockResponse, $decodingOption, $url);
    }

    public function testRequestLevelConfigOverridesGlobal()
    {
        $url = $this->defaultUrl."&%23someId=1";
        $expectedDecoded = $this->defaultUrl."&#someId=1";

        $mockResponse = [
            function (RequestInterface $request, array $options) use ($expectedDecoded) {
                $this->assertEquals($expectedDecoded, strval($request->getUri()));
                return new Response(200);
            }
        ];
        $decodingOption = new ParametersDecodingOption(['#']);
        $requestOptions = [
            ParametersDecodingOption::class => $decodingOption
        ];
        $this->executeMockRequest($mockResponse, new ParametersDecodingOption(), $url, $requestOptions);
    }

    private function executeMockRequest(array $mockResponses, ?ParametersDecodingOption $decodingOption = null, string $url = null, array $requestOptions = []): ResponseInterface
    {
        $mockHandler = new MockHandler($mockResponses);
        $handlerStack = new HandlerStack($mockHandler);
        $decodingOption = $decodingOption ?: new ParametersDecodingOption();
        $handlerStack->push(KiotaMiddleware::parameterNamesDecoding($decodingOption));

        $guzzle = new Client(['handler' => $handlerStack]);
        $url = $url ?: $this->defaultUrl;
        return $guzzle->get($url, $requestOptions);
    }
}
