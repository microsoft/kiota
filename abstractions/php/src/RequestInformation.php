<?php
namespace Microsoft\Kiota\Abstractions;

use http\Url;
use Psr\Http\Message\StreamInterface;

class RequestInformation {

    /** @var string|null */
    public ?string $uri;

    /** @var HttpMethod|null */
    public ?HttpMethod $httpMethod;

    /** @var StreamInterface $content */
    public StreamInterface $content;

    /** @var array $headers */
    public array $headers = [];

    /** @var array $queryParams */
    public array $queryParams = [];

    /** @var string $binaryContentType */
    private static string $binaryContentType = "application/octet-stream";

    /** @var string $contentTypeHeader */
    private static string $contentTypeHeader = "Content-Type";

    /** @var array */
    private array $_middlewareOptions = [];

    /**
     * @param StreamInterface $value
     */
    public function setStreamContent(StreamInterface $value): void {
        $this->content = $value;
        $this->headers[self::$contentTypeHeader] = self::$binaryContentType;
    }

    /**
     * @param HttpCore $httpCore
     * @param string $contentType
     * @param object ...$values
     */
    public function setContentFromParsable(HttpCore $httpCore, string $contentType, object ...$values): void {
        if(count($values) === 0) {
            throw new \RuntimeException('$values cannot be empty');
        }

        try {
            $writer = $httpCore->getSerializationWriterFactory()
                               ->getSerializationWriter($contentType);
            $this->headers[self::$contentTypeHeader] = $contentType;

            if(count($values) === 1){
                $writer->writeObjectValue(null, $values[0]);
            } else {
                $writer->writeCollectionOfObjectValues(null, $values);
            }

        } catch (\RuntimeException $ex) {
            throw new \RuntimeException('Could not serialize payload ', 0, $ex);
        }
    }

    /**
     * @param string $uriString
     */
    public function setUriFromString(string $uriString): void {
        $this->uri = http_build_url(parse_url($uriString));

        if (!$this->uri){
            throw new \RuntimeException;
        }
    }

    /**
     * @param MiddlewareOption ...$options
     */
    public function removeMiddlewareOptions(MiddlewareOption ...$options): void {
        foreach ($options as $middlewareOption) {
            unset($options[get_class($middlewareOption)]);
        }
    }

    /**
     * @param MiddlewareOption ...$options
     */
    public function addMiddlewareOptions(MiddlewareOption ...$options): void {

    }
}
