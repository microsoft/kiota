<?php

namespace Microsoft\Kiota\Http\Test\Middleware;

use GuzzleHttp\Client;
use GuzzleHttp\Handler\MockHandler;
use GuzzleHttp\HandlerStack;
use GuzzleHttp\Psr7\Response;
use Microsoft\Kiota\Http\Middleware\ChaosHandler;
use Microsoft\Kiota\Http\Middleware\KiotaMiddleware;
use Microsoft\Kiota\Http\Middleware\Options\ChaosOption;
use PHPUnit\Framework\TestCase;
use Psr\Http\Message\RequestInterface;

class ChaosHandlerTest extends TestCase
{
    public function testRandomResponseIsSelectedFromChaosOptions()
    {
        $chaosOptions = new ChaosOption([
            new Response(404),
            function (RequestInterface $request, array $options) {
                return new Response(500);
            }
        ], 100);

        $chaosClient = $this->getChaosClient($chaosOptions);
        $chaosResponse = $chaosClient->get("/");
        $this->assertTrue($chaosResponse->getStatusCode() > 200); // mock handler returns 200s only
    }

    public function testDefaultResponseIsSelectedIfNoChaosResponses()
    {
        $chaosOptions = new ChaosOption([], 100);
        $chaosClient = $this->getChaosClient($chaosOptions);
        $response = $chaosClient->get("/");
        $this->assertInstanceOf(Response::class, $response);
    }

    public function testRequestLevelConfigOverridesClientLevelConfig()
    {
        $requestChaosOptions = new ChaosOption([
            new Response(300),
            function (RequestInterface $request, array $options) {
                return new Response(300);
            }
        ], 100);
        $requestOption = [
            ChaosOption::class => $requestChaosOptions
        ];
        $chaosClient = $this->getChaosClient(new ChaosOption());
        $chaosResponse = $chaosClient->get("/", $requestOption);
        $this->assertTrue($chaosResponse->getStatusCode() == 300);
    }

    private function getChaosClient(ChaosOption $chaosOption): Client
    {
        $mockHandler = new MockHandler([
            new Response(200),
            new Response(200),
            new Response(200),
            new Response(200),
            new Response(200)
        ]);
        $handlerStack = new HandlerStack($mockHandler);
        $handlerStack->push(KiotaMiddleware::chaos($chaosOption));
        return new Client(['handler' => $handlerStack, 'http_errors' => false]);
    }


}
