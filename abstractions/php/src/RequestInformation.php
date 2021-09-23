<?php
namespace Microsoft\Kiota\Abstractions;

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

    /** @var array<string, mixed> */
    private array $_middlewareOptions = [];

    /**
     * Sets the request body to be binary stream.
     * @param StreamInterface $value The Binary stream
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
     * Sets a request URI from the given string
     * @param string $uriString the string to use to construct the URI.
     */
    public function setUriFromString(string $uriString): void {
        $this->uri = http_build_url(parse_url($uriString));

        if (!$this->uri){
            throw new \RuntimeException;
        }
    }

    /**
     * @param string|null $currentPath
     * @param string|null $pathSegment
     * @param bool $isRawUri
     */
    public function setUri(?string $currentPath, ?string $pathSegment, bool $isRawUri): void {
        if ($isRawUri) {
            if ($currentPath === null || empty(trim($currentPath))) {
                throw new \InvalidArgumentException('$currentPath cannot be null or empty');
            }

            $urls = parse_url($currentPath);

            if (!$urls) {
                throw new \InvalidArgumentException('Invalid url provided');
            }
            $schemeHostAndPath = $urls['scheme'] . '://'.$urls['host'] . $urls['path'];

            $requestParameters = [];

            if (array_key_exists('query', $urls)) {
                parse_str($urls['query'], $requestParameters);
            }
            foreach ($requestParameters as $requestParameter => $requestParameterValue) {
                if ($requestParameter !== null && !empty(trim($requestParameter))) {
                    $this->queryParams[$requestParameter] = $requestParameterValue;
                }
            }
            $this->setUriFromString($schemeHostAndPath);

        } else {
            $this->setUriFromString($currentPath . $pathSegment);
        }
    }

    /**
     * Removes middleware options from this request
     * @param MiddlewareOption ...$options The middleware options to remove.
     */
    public function removeMiddlewareOptions(MiddlewareOption ...$options): void {
        foreach ($options as $middlewareOption) {
            unset($this->_middlewareOptions[get_class($middlewareOption)]);
        }
    }

    /**
     * Adds a middleware option to this request.
     * @param MiddlewareOption ...$options The middleware options to add.
     */
    public function addMiddlewareOptions(MiddlewareOption ...$options): void {
        foreach ($options as $middlewareOption) {
            $this->_middlewareOptions[get_class($middlewareOption)] = $middlewareOption;
        }
    }

    /**
     * Gets the middleware options for this request. Options are unique by type
     * If an option of the same type is added twice, the last one wins.
     * @return array The middleware options in this request.
     */
    public function getMiddlewareOptions(): array {
        return $this->_middlewareOptions;
    }
}
