<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware;

use GuzzleHttp\Exception\BadResponseException;
use GuzzleHttp\Promise\Create;
use GuzzleHttp\Promise\PromiseInterface;
use Microsoft\Kiota\Http\Middleware\Options\RetryOption;
use Psr\Http\Message\RequestInterface;
use Psr\Http\Message\ResponseInterface;

/**
 * Class RetryHandler
 *
 * Middleware that retries requests based on {@link RetryOption} while respecting the Retry-After header
 *
 * @package Microsoft\Kiota\Http\Middleware
 * @copyright 2021 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class RetryHandler
{
    private const RETRY_AFTER_HEADER = "Retry-After";
    private const RETRY_ATTEMPT_HEADER = "Retry-Attempt";

    /**
     * @var RetryOption configuration options for the middleware
     */
    private RetryOption $retryOption;
    /**
     * @var callable(RequestInterface, array): PromiseInterface
     */
    private $nextHandler;

    /**
     * @param RetryOption|null $retryOption
     * @param callable $nextHandler
     */
    public function __construct(?RetryOption $retryOption, callable $nextHandler)
    {
        $this->retryOption = ($retryOption) ?: new RetryOption();
        $this->nextHandler = $nextHandler;
    }

    /**
     * @param RequestInterface $request
     * @param array $options
     * @return PromiseInterface
     */
    public function __invoke(RequestInterface $request, array $options): PromiseInterface
    {
        $fn = $this->nextHandler;
        return $fn($request, $options)->then(
            $this->onFulfilled($request, $options),
            $this->onRejected($request, $options)
        );
    }

    /**
     * Handles retry for a successful request
     *
     * @param RequestInterface $request
     * @param array $options
     * @return callable
     */
    private function onFulfilled(RequestInterface $request, array $options): callable
    {
        return function (ResponseInterface $response) use ($request, $options) {
            $retries = $this->getRetries($request);
            $delaySecs = $this->calculateDelay($retries, $response);
            if (!$this->shouldRetry($request, $retries, $delaySecs, $response)
                || $this->exceedRetriesTimeLimit($delaySecs)) {
                return $response;
            }
            $options['delay'] = $delaySecs; // Guzzle sleeps the thread before executing request
            $request = $request->withHeader(self::RETRY_ATTEMPT_HEADER, $retries ++);
            $request->getBody()->rewind();
            return $this($request, $options);
        };
    }

    /**
     * Handles retry if {@link BadResponseException} is thrown by Guzzle
     * BadResponseException is thrown for 4xx and 5xx responses if configured on Guzzle client
     *
     * @param RequestInterface $request
     * @param array $options
     * @return callable
     */
    private function onRejected(RequestInterface $request, array $options): callable
    {
        return function ($reason) use ($request, $options) {
            // No retry for network-related/other exceptions
            if (!is_a(\GuzzleHttp\Exception\BadResponseException::class, $reason)) {
                Create::rejectionFor($reason);
            }

            $retries = $this->getRetries($request);
            $delaySecs = $this->calculateDelay($retries, $reason->getResponse());
            if (!$this->shouldRetry($request, $retries, $delaySecs, $reason->getResponse())
                 || $this->exceedRetriesTimeLimit($delaySecs)) {
                Create::rejectionFor($reason);
            }
            $options['delay'] = $delaySecs; // Guzzle sleeps the thread before executing request
            $request = $request->withHeader(self::RETRY_ATTEMPT_HEADER, $retries ++);
            $request->getBody()->rewind();
            return $this($request, $options);
        };
    }

    /**
     * Returns true if request should be retried
     *
     * @param RequestInterface $request
     * @param int $retries
     * @param int $delaySecs
     * @param ResponseInterface|null $response
     * @return bool
     */
    private function shouldRetry(RequestInterface $request, int $retries, int $delaySecs, ?ResponseInterface $response): bool
    {
        return (($retries < $this->retryOption->getMaxRetries())
                    && $this->isPayloadRewindable($request)
                    && $this->retryOption->getShouldRetry()($delaySecs, $retries, $response)
                    && (!$response || $this->isRetryStatusCode($response->getStatusCode()))
        );
    }

    /**
     * Get number of retries from the $request Retry-Attempt header
     *
     * @param RequestInterface $request
     * @return int
     */
    private function getRetries(RequestInterface $request): int
    {
        if ($request->hasHeader(self::RETRY_ATTEMPT_HEADER)) {
            return intval($request->getHeader(self::RETRY_ATTEMPT_HEADER)[0]);
        }
        return 0;
    }

    /**
     * Determine delay in seconds based on $retryOptions, total number of retries and Retry-After header value
     *
     * @param int $retries
     * @param ResponseInterface|null $response
     * @return int seconds to delay
     */
    private function calculateDelay(int $retries, ?ResponseInterface $response): int
    {
        $retryAfterSeconds = 0;
        if ($response && $response->hasHeader(self::RETRY_AFTER_HEADER)) {
            $retryAfterSeconds = $this->parseRetryAfterToSeconds($response->getHeader(self::RETRY_AFTER_HEADER)[0]);
        }

        // First retry attempt
        if ($retries == 0) {
            return ($this->retryOption->getDelay() > $retryAfterSeconds) ? $this->retryOption->getDelay() : $retryAfterSeconds;
        }

        $retries ++;
        $expoDelay = $this->exponentialDelay($retries);
        return ($expoDelay > $retryAfterSeconds) ? $expoDelay : $retryAfterSeconds;
    }

    /**
     * Returns true if $delaySeconds exceeds {@link RetryOption} retriesTimeLimit
     *
     * @param int $delaySecs
     * @return bool
     */
    private function exceedRetriesTimeLimit(int $delaySecs): bool
    {
        if (!$this->retryOption->getRetriesTimeLimit()) {
            return false;
        }
        // Add intervals to 01 Jan 1970 00:00:00
        $retriesLimitSecs = date_create("@0")->add($this->retryOption->getRetriesTimeLimit())->getTimestamp();
        $processingSecs = date_create("@0")->getTimestamp() + $delaySecs;
        return ($processingSecs > $retriesLimitSecs);
    }

    /**
     * Returns true if Http Status Code warrants a retry
     *
     * @param int $httpStatusCode
     * @return bool
     */
    private function isRetryStatusCode(int $httpStatusCode): bool
    {
        return ($httpStatusCode == 429 || $httpStatusCode == 503 || $httpStatusCode == 504);
    }

    /**
     * Returns true if request payload is a rewindable stream
     *
     * @param RequestInterface $request
     * @return bool
     */
    private function isPayloadRewindable(RequestInterface $request): bool
    {
        return ($request->getBody() && $request->getBody()->isSeekable());
    }

    /**
     * Parses Http Retry-After values of type <http-date> of <delay-seconds>
     *
     * @param string $retryAfterValue Retry-After value formatted as <http-date> or <delay-seconds>
     * @return int number of seconds
     */
    private function parseRetryAfterToSeconds(string $retryAfterValue): int
    {
        if (is_numeric($retryAfterValue)) {
            return intval($retryAfterValue);
        }
        $retryAfterDateTime = \DateTime::createFromFormat(\DateTimeInterface::RFC7231, $retryAfterValue);
        if (!$retryAfterDateTime) {
            throw new \RuntimeException("Unable to parse Retry-After header value $retryAfterValue");
        }
        return $retryAfterDateTime->getTimestamp() - (new \DateTime())->getTimestamp();
    }

    /**
     * Exponential backoff delay function.
     *
     * @return int seconds.
     */
    private function exponentialDelay(int $retries): int
    {
        return (int) \pow(2, $retries - 1) * $this->retryOption->getDelay();
    }
}
