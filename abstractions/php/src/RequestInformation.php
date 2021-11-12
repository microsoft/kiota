<?php
namespace Microsoft\Kiota\Abstractions;

use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Psr\Http\Message\StreamInterface;

class RequestInformation {

    /** @var string The url template for the current request.  */
    public string $urlTemplate;
    /** @var string The URI of the request. */
    public string $uri;

    private const RAW_URL_KEY = 'request-raw-url';

    /** @var string|null The HTTP method for the request */
    public ?string $httpMethod;

    /** @var StreamInterface $content The Request Body. */
    public StreamInterface $content;

    /** @var array<string,string[]|string> $headers  The Request Headers. */
    public array $headers = [];

    /** @var array<string,mixed> $queryParams The Query Parameters of the request. */
    public array $queryParams = [];

    /** @var string $binaryContentType */
    private static string $binaryContentType = "application/octet-stream";

    /** @var string $contentTypeHeader */
    private static string $contentTypeHeader = "Content-Type";

    /** @var array<string, RequestOption> Request options to apply to this request. */
    private array $_requestOptions = [];

    /**
     * Sets the request body to be binary stream.
     * @param StreamInterface $value The Binary stream
     */
    public function setStreamContent(StreamInterface $value): void {
        $this->content = $value;
        $this->headers[self::$contentTypeHeader] = self::$binaryContentType;
    }

    /**
     * @param RequestAdapter $httpCore
     * @param string $contentType
     * @param Parsable|object ...$values
     */
    public function setContentFromParsable(RequestAdapter $httpCore, string $contentType, ...$values): void {
        /** @var array<Parsable|object> $newValues */
        $newValues = [];

        foreach ($values as $key => $value) {
            $newValues []= $value;
        }
        if(count($newValues) === 0) {
            throw new \RuntimeException('$values cannot be empty');
        }

        $values = array_values($newValues);
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
     * Sets the URI of the request.
     * @param string|null $currentPath the current path (scheme, host, port, path, query parameters) of the request.
     * @param string|null $pathSegment the segment to append to the current path.
     * @param bool $isRawUri whether the path segment is a raw url. When true, the segment is not happened and the current path is parsed for query parameters.
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
     * @param RequestOption ...$options The middleware options to remove.
     */
    public function removeRequestOptions(RequestOption ...$options): void {
        foreach ($options as $middlewareOption) {
            unset($this->_requestOptions[get_class($middlewareOption)]);
        }
    }

    /**
     * Adds a middleware option to this request.
     * @param RequestOption ...$options The middleware options to add.
     */
    public function addRequestOptions(RequestOption ...$options): void {
        foreach ($options as $requestOption) {
            $this->_requestOptions[get_class($requestOption)] = $requestOption;
        }
    }

    /**
     * Gets the middleware options for this request. Options are unique by type
     * If an option of the same type is added twice, the last one wins.
     * @return array<RequestOption> The middleware options in this request.
     */
    public function getRequestOptions(): array {
        return $this->_requestOptions;
    }
}
