<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware;

use GuzzleHttp\Promise\Promise;
use GuzzleHttp\Promise\PromiseInterface;
use Microsoft\Kiota\Http\Middleware\Options\RetryOption;
use Psr\Http\Message\RequestInterface;
use Psr\Http\Message\ResponseInterface;

/**
 * Class RetryHandler
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
     * @var int Seconds to delay after parsing Retry-After header and comparing it with $retryOption delay
     */
    private int $delaySecs;

    /**
     * @param RetryOption|null $retryOption
     * @param callable $nextHandler
     */
    public function __construct(?RetryOption $retryOption, callable $nextHandler)
    {
        $this->retryOption = ($retryOption) ?: new RetryOption();
        $this->nextHandler = $nextHandler;
    }

    public function __invoke(RequestInterface $request, array $options): PromiseInterface
    {
        $fn = $this->nextHandler;
        return $fn($request, $options)->then(
            $this->onFulfilled($request, $options),
            function ($reason) {
                throw $reason;
            }
        );
    }

    private function onFulfilled(RequestInterface $request, array $guzzleOptions): callable
    {
        return function (ResponseInterface $response) use ($request, $guzzleOptions) {
            $startTimeSecs = (new \DateTime())->getTimestamp();
            if (!$this->isRetryStatusCode($response->getStatusCode())
                || (array_key_exists('retries', $guzzleOptions) &&  $guzzleOptions['retries'] > $this->retryOption->getMaxRetries())
                || !$request->getBody()->isSeekable()
            ) {
                return $response;
            }

            $delayPromise = $this->delay($response, $guzzleOptions);

            if (!$this->retryOption->getShouldRetry()($this->delaySecs, $guzzleOptions['retries'], $response)) {
                $delayPromise->cancel();
                return $response;
            }

            // Check retriesTimeLimit
            $processingTimeSecs = (new \DateTime())->getTimestamp() - $startTimeSecs;
            $retriesLimitSecs = date_create("@0")->add($this->retryOption->getRetriesTimeLimit())->getTimestamp();
            if (($processingTimeSecs + $this->delaySecs) > $retriesLimitSecs) {
                $delayPromise->cancel();
                return $response;
            }

            if (array_key_exists('retries', $guzzleOptions)) {
                $guzzleOptions['retries'] ++;
            } else {
                $guzzleOptions['retries'] = 1;
            }
            $request = $request->withAddedHeader(self::RETRY_ATTEMPT_HEADER, strval($guzzleOptions['retries']));
            $request->getBody()->rewind();
            $delayPromise->wait();
            return $this($request, $guzzleOptions);
        };
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
     * Determines the correct delay period and sleeps
     *
     * @param ResponseInterface $response
     * @param array $options
     * @return PromiseInterface
     */
    private function delay(ResponseInterface $response, array $options): PromiseInterface
    {
        $this->delaySecs = $this->retryOption->getDelay();
        if ($response->hasHeader(self::RETRY_AFTER_HEADER)) {
            $retryAfterValue = $response->getHeader(self::RETRY_AFTER_HEADER)[0];
            $retryAfterSeconds = $this->parseRetryAfterToSeconds($retryAfterValue);
            $this->delaySecs = ($this->delaySecs > $retryAfterSeconds) ?: $retryAfterSeconds;
        } else {
            $this->delaySecs = $this->exponentialDelay($options['retries'] + 1);
        }
        return new Promise(
            function () {
                sleep($this->delaySecs);
            }
        );
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
            return strval($retryAfterValue);
        }
        $retryAfterDateTime = \DateTime::createFromFormat(\DateTimeInterface::RFC7231, $retryAfterValue);
        if (!$retryAfterDateTime) {
            throw new \RuntimeException("Unable to parse Retry-After header value $retryAfterValue");
        }
        return $retryAfterDateTime->getTimestamp() - (new \DateTime())->getTimestamp();
    }

    /**
     * Default exponential backoff delay function.
     *
     * @return int milliseconds.
     */
    private function exponentialDelay(int $retries): int
    {
        return (int) \pow(2, $retries - 1) * $this->retryOption->getDelay();
    }
}
