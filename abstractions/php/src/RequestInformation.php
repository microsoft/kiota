<?php
namespace Microsoft\Kiota\Abstractions;

use Exception;
use InvalidArgumentException;
use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Psr\Http\Message\StreamInterface;
use Rize\UriTemplate;
use RuntimeException;

class RequestInformation {
    /** @var string $RAW_URL_KEY */
    public static string $RAW_URL_KEY = 'request-raw-url';
    /** @var string $urlTemplate The url template for the current request */
    public string $urlTemplate;
    /**
     * The path parameters for the current request
     * @var array<string,mixed> $pathParameters
     */
    public array $pathParameters = [];

    /** @var string $uri */
    private string $uri;
    /**
     * @var string The HTTP method for the request
     */
    public string $httpMethod;
    /** @var array<string,mixed> The Query Parameters of the request. */
    public array $queryParameters = [];
    /** @var array<string, mixed>  The Request Headers. */
    public array $headers = [];
    /** @var StreamInterface|null $content The Request Body. */
    public ?StreamInterface $content = null;
    /** @var array<string,RequestOption> */
    private array $requestOptions = [];
    /** @var string $binaryContentType */
    private static string $binaryContentType = 'application/octet-stream';
    /** @var string $contentTypeHeader */
    public static string $contentTypeHeader = 'Content-Type';

    /** Gets the URI of the request.
     * @return string
     */
    public function getUri(): string {
        if (!empty($this->uri)) {
            return $this->uri;
        }
        if(array_key_exists(self::$RAW_URL_KEY, $this->pathParameters)
            && is_string($this->pathParameters[self::$RAW_URL_KEY])) {
            $this->setUri($this->pathParameters[self::$RAW_URL_KEY]);
        } else {
            $template = (new UriTemplate());
            $params = array_merge($this->pathParameters, $this->queryParameters);
            return $template->expand($this->urlTemplate, $params);
        }
        return $this->uri;
    }

    /**
     * Sets the URI of the request.
     */
    public function setUri(string $uri): void {
        if (empty($uri)) {
            throw new InvalidArgumentException('$uri cannot be empty.');
        }
        $this->uri = $uri;
        $this->queryParameters = [];
        $this->pathParameters = [];
    }

    /**
     * Gets the request options for this request. Options are unique by type. If an option of the same type is added twice, the last one wins.
     * @return array<string,RequestOption> the request options for this request.
     */
    public function getRequestOptions(): array {
        return $this->requestOptions;
    }

    /**
     * Adds request option(s) to this request.
     * @param RequestOption ...$options the request option to add.
     */
    public function addRequestOptions(RequestOption ...$options): void {
        if (empty($options)) {
            return;
        }
        foreach ($options as $option) {
            $this->requestOptions[get_class($option)] = $option;
        }
    }

    /**
     * Removes request option(s) from this request.
     * @param RequestOption ...$options the request option to remove.
     */
    public function removeRequestOptions(RequestOption ...$options): void {
        foreach ($options as $option) {
            unset($this->requestOptions[get_class($option)]);
        }
    }

    /**
     * Sets the request body to be a binary stream.
     * @param StreamInterface $value the binary stream
     */
    public function setStreamContent(StreamInterface $value): void {
        $this->content = $value;
        $this->headers[self::$contentTypeHeader] = self::$binaryContentType;
    }

    /**
     * Sets the request body from a model with the specified content type.
     * @param RequestAdapter $requestAdapter The adapter service to get the serialization writer from.
     * @param string $contentType the content type.
     * @param Parsable ...$values the models.
     */
    public function setContentFromParsable(RequestAdapter $requestAdapter, string $contentType, Parsable ...$values): void {
        if (empty($values)) {
            throw new InvalidArgumentException('$values cannot be empty.');
        }

        try {
            $writer = $requestAdapter->getSerializationWriterFactory()->getSerializationWriter($contentType);
            $this->headers[self::$contentTypeHeader] = $contentType;

            if (count($values) === 1) {
                $writer->writeObjectValue(null, $values[0]);
            } else {
                $writer->writeCollectionOfObjectValues(null, $values);
            }
            $this->content = $writer->getSerializedContent();
        } catch (Exception $ex) {
            throw new RuntimeException('could not serialize payload.', 1, $ex);
        }
    }

    /**
     * Set the query parameters.
     * @param array<string,mixed> $queryParameters
     */
    public function setQueryParameters(array $queryParameters): void {
        $this->queryParameters = $queryParameters;
    }

    /**
     * Set the path parameters.
     * @param array<string,mixed> $pathParameters
     */
    public function setPathParameters(array $pathParameters): void {
        $this->pathParameters = $pathParameters;
    }
}
