<?php
namespace Microsoft\Kiota\Abstractions;

use Psr\Http\Message\StreamInterface;

class RequestInfo {
    public ?string $uri;
    public ?HttpMethod $httpMethod;
    public StreamInterface $contents;
    public array $headers = [];
    public array $queryParams = [];
    private static string $binaryContentType = "application/octet-stream";
    private static string $contentTypeHeader = "Content-Type";

    public function setStreamContent(StreamInterface $value): void {
        $this->contents = $value;
        $this->headers[self::$contentTypeHeader] = self::$binaryContentType;
    }
}
