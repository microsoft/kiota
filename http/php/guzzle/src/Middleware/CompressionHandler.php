<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware;

use GuzzleHttp\Promise\Create;
use GuzzleHttp\Promise\PromiseInterface;
use Microsoft\Kiota\Http\Middleware\Options\CompressionOption;
use Psr\Http\Message\RequestInterface;
use Psr\Http\Message\ResponseInterface;

/**
 * Class CompressionHandler
 *
 * Compresses a request body using the provided callbacks in {@link CompressionOption}
 * Should the server return a 415, the CompressionHandler retries the request ONLY once with an uncompressed body
 *
 * @package Microsoft\Kiota\Http\Middleware
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class CompressionHandler
{
    private const COMPRESSION_RETRY_ATTEMPT = 'compressionRetryAttempt';

    /**
     * @var CompressionOption {@link CompressionOption}
     */
    private CompressionOption $compressionOption;

    /**
     * @var callable(RequestInterface, array): PromiseInterface
     * Next handler to be called in the middleware pipeline
     */
    private $nextHandler;

    /**
     * @var RequestInterface Initial request with uncompressed body
     */
    private RequestInterface $originalRequest;

    /**
     * @param callable $nextHandler
     * @param CompressionOption|null $compressionOption
     */
    public function __construct(callable $nextHandler, ?CompressionOption $compressionOption = null)
    {
        $this->nextHandler = $nextHandler;
        $this->compressionOption = ($compressionOption) ?: new CompressionOption();
    }

    /**
     * @param RequestInterface $request
     * @param array $options
     * @return PromiseInterface
     */
    public function __invoke(RequestInterface $request, array $options): PromiseInterface
    {
        $this->originalRequest = $request; // keep reference in case we have to retry with uncompressed body
        if (!$this->shouldRetry($options)) {
            $request = $this->compress($request);
        }
        $fn = $this->nextHandler;
        return $fn($request, $options)->then(
            $this->onFulfilled($options),
            $this->onRejected($options)
        );
    }

    /**
     * if true, the request is retried with an uncompressed body
     *
     * @param array $options
     * @return bool
     */
    private function shouldRetry(array $options): bool
    {
        return (array_key_exists(self::COMPRESSION_RETRY_ATTEMPT, $options) && $options[self::COMPRESSION_RETRY_ATTEMPT] == 1);
    }

    /**
     * Retries the request if 415 response was received
     *
     * @param array $options
     * @return callable
     */
    private function onFulfilled(array $options): callable
    {
        return function (ResponseInterface $response) use ($options) {
            if ($response->getStatusCode() == 415 && !array_key_exists(self::COMPRESSION_RETRY_ATTEMPT, $options)) {
                $options[self::COMPRESSION_RETRY_ATTEMPT] = 1;
                return $this($this->originalRequest, $options);
            }
            return $response;
        };
    }

    /**
     * Retry only if guzzle BadResponseException was thrown with a 415 status code
     *
     * @param array $options
     * @return callable
     */
    private function onRejected(array $options): callable
    {
        return function ($reason) use ($options) {
            // Only consider 415 BadResponseException in case guzzle http_errors = true
            if (is_a($reason, \GuzzleHttp\Exception\BadResponseException::class)) {
                if ($reason->getResponse()->getStatusCode() == 415 && !array_key_exists(self::COMPRESSION_RETRY_ATTEMPT, $options)) {
                    $options[self::COMPRESSION_RETRY_ATTEMPT] = 1;
                    return $this($this->originalRequest, $options);
                }
            }
            return Create::rejectionFor($reason);
        };
    }

    /**
     * Applies compression callbacks provided in {@link CompressionOption} to the request
     *
     * @param RequestInterface $request
     * @return RequestInterface
     */
    private function compress(RequestInterface $request): RequestInterface
    {
        foreach ($this->compressionOption->getCallbacks() as $callback) {
            $request = $callback($request);
        }
        return $request;
    }
}
