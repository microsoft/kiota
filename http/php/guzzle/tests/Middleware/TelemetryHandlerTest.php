<?php

namespace Microsoft\Kiota\Http\Test\Middleware;

use GuzzleHttp\Client;
use GuzzleHttp\Handler\MockHandler;
use GuzzleHttp\HandlerStack;
use GuzzleHttp\Psr7\Response;
use Microsoft\Kiota\Http\Middleware\KiotaMiddleware;
use Microsoft\Kiota\Http\Middleware\Options\TelemetryOption;
use Microsoft\Kiota\Http\Middleware\TelemetryHandler;
use PHPUnit\Framework\TestCase;
use Psr\Http\Message\RequestInterface;

class TelemetryHandlerTest extends TestCase
{
    public function testTelemetry()
    {
        $telemetryConfigurator = function (RequestInterface $request) {
            return $request->withHeader('SdkVersion', '1.0');
        };
        $telemetryOption = new TelemetryOption($telemetryConfigurator);
        $mockResponse = [
            function (RequestInterface $request, array $options) {
                if ($request->hasHeader('SdkVersion') && $request->getHeaderLine('SdkVersion') === '1.0') {
                    return new Response(200);
                }
                throw new \RuntimeException("Telemetry header not set");
            }
        ];
        $response = $this->executeMockRequest($mockResponse, $telemetryOption);
        $this->assertEquals(200, $response->getStatusCode());
    }

    public function testTelemetryWithNullCallback()
    {
        $telemetryOption = new TelemetryOption(null);
        $mockResponse = [new Response(200)];
        $response = $this->executeMockRequest($mockResponse, $telemetryOption);
        $this->assertEquals(200, $response->getStatusCode());
    }

    private function executeMockRequest(array $mockResponses, ?TelemetryOption $telemetryOption = null, ?array $requestOptions = [])
    {
        $mockHandler = new MockHandler($mockResponses);
        $handlerStack = new HandlerStack($mockHandler);
        $handlerStack->push(KiotaMiddleware::telemetry($telemetryOption));

        $guzzleClient = new Client(['handler' => $handlerStack, 'http_errors' => false]);
        return $guzzleClient->get("/", $requestOptions);
    }
}
