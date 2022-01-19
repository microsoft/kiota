<?php

namespace Microsoft\Kiota\Http\Test\Middleware;

use GuzzleHttp\Client;
use GuzzleHttp\Handler\MockHandler;
use GuzzleHttp\HandlerStack;
use GuzzleHttp\Psr7\Response;
use Microsoft\Kiota\Http\Middleware\KiotaMiddleware;
use Microsoft\Kiota\Http\Middleware\Options\CompressionOption;
use PHPUnit\Framework\TestCase;
use Psr\Http\Message\RequestInterface;

class CompressionHandlerTest extends TestCase
{
    public function testGzipCompression()
    {
        $initialRequestBody = "body";
        $mockResponse = [
            function (RequestInterface $request, array $options) use ($initialRequestBody) {
                $decompressedBody = gzdecode($request->getBody()->getContents());
                if ($decompressedBody === $initialRequestBody
                    && $request->hasHeader('Content-Encoding')
                    && $request->getHeaderLine('Content-Encoding') === 'gzip') {
                    return new Response(200);
                }
                throw new \RuntimeException("Decompressed body doesn't match initial request body");
            }
        ];

        $response = $this->executeMockRequest($mockResponse, null, ['body' => $initialRequestBody]); // gzip is default
        $this->assertEquals(200, $response->getStatusCode());
    }

    public function testDeflateCompression()
    {
        $initialRequestBody = "body";
        $mockResponse = [
            function (RequestInterface $request, array $options) use ($initialRequestBody) {
                $decompressedBody = gzinflate($request->getBody()->getContents());
                if ($decompressedBody === $initialRequestBody
                    && $request->hasHeader('Content-Encoding')
                    && $request->getHeaderLine('Content-Encoding') === 'deflate') {
                    return new Response(200);
                }
                throw new \RuntimeException("Decompressed body doesn't match initial request body");
            }
        ];

        $response = $this->executeMockRequest($mockResponse, new CompressionOption([CompressionOption::deflate()]), ['body' => $initialRequestBody]); // gzip is default
        $this->assertEquals(200, $response->getStatusCode());
    }

    public function testAtMostOneRetry()
    {
        $mockResponse = [
            new Response(415),
            new Response(200)
        ];
        $response = $this->executeMockRequest($mockResponse, null, ['body' => "Request body"]);
        $this->assertEquals(200, $response->getStatusCode());
    }

    public function testBadResponseExceptionCausesAtMostOneRetry()
    {
        $mockResponse = [
            new Response(415),
            new Response(200)
        ];
        $response = $this->executeMockRequest($mockResponse, null, ['body' => "Request body", 'http_errors' => true]);
        $this->assertEquals(200, $response->getStatusCode());
    }

    public function testMultipleCompressionCallbacks()
    {
        $body = "payload";
        $compressionOptions = [CompressionOption::gzip(), CompressionOption::deflate()];
        $mockResponse = [
            function (RequestInterface $request, array $options) use ($body) {
                $decompressedBody = gzdecode(gzinflate($request->getBody()->getContents()));
                if ($decompressedBody === $body
                    && $request->hasHeader('Content-Encoding')
                    && $request->getHeaderLine('Content-Encoding') === 'gzip, deflate') {
                    return new Response(200);
                }
                throw new \RuntimeException("Decompressed body doesn't match initial request body");
            }
        ];
        $response = $this->executeMockRequest($mockResponse, new CompressionOption($compressionOptions), ['body' => $body]);
        $this->assertEquals(200, $response->getStatusCode());
    }

    private function executeMockRequest(array $mockResponses, ?CompressionOption $compressionOption = null, ?array $requestOptions = [])
    {
        $mockHandler = new MockHandler($mockResponses);
        $handlerStack = new HandlerStack($mockHandler);
        $handlerStack->push(KiotaMiddleware::compression($compressionOption));

        $guzzleClient = new Client(['handler' => $handlerStack, 'http_errors' => false]);
        return $guzzleClient->get("/", $requestOptions);
    }
}
