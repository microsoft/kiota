<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware\Options;

use Microsoft\Kiota\Http\Middleware\RetryHandler;
use Psr\Http\Message\ResponseInterface;

/**
 * Class RetryOption
 *
 * Options that configure {@link RetryHandler}
 *
 * @package Microsoft\Kiota\Http\Middleware\Options
 * @copyright 2021 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class RetryOption
{
    /**
     * Upper limit on {@link $maxRetries}
     */
    private const MAX_MAX_RETRIES = 10;
    /**
     * Upper limit on {@link $delaySeconds}
     */
    private const MAX_DELAY_SEC = 180;

    /**
     * @var int Max number of times a request can be resent
     */
    private int $maxRetries = 3;
    /**
     * @var int Minimum amount of time to wait before retrying
     * This changes if the ResponseInterface contains a Retry-After header.
     * If $delayMilliseconds < Retry-After value, delay is set to Retry-After value
     */
    private int $delaySeconds = 3;
    /**
     * @var \DateInterval Cumulative retry time (Retry-After + Retry request time) after which the previous failed response is returned
     */
    private \DateInterval $retriesTimeLimit;
    /**
     * @var callable(int, int, ResponseInterface):bool $sh Custom callback function to determine if request should be retried
     * The callback should accept a delay time in seconds, number of retry attempts and a {@link ResponseInterface} and return a bool
     */
    private $shouldRetry;

    public function __construct()
    {
        // Set defaults
        $this->retriesTimeLimit = new \DateInterval("0");
        $this->shouldRetry = function ($delaySec, $retries, $response) {
            return true;
        };
    }

    /**
     * @param int $maxRetries
     * @return RetryOption
     */
    public function setMaxRetries(int $maxRetries): self
    {
        if ($maxRetries > self::MAX_MAX_RETRIES) {
            throw new \InvalidArgumentException("Max retries exceeded maximum limit of ".self::MAX_MAX_RETRIES);
        }
        $this->maxRetries = $maxRetries;
        return $this;
    }

    /**
     * @return int
     */
    public function getMaxRetries(): int
    {
        return $this->maxRetries;
    }

    /**
     * @param int $delaySeconds
     * @return RetryOption
     */
    public function setDelay(int $delaySeconds): self
    {
        if ($delaySeconds > self::MAX_DELAY_SEC) {
            throw new \InvalidArgumentException("Delay exceeds max delay limit of ".self::MAX_DELAY_SEC);
        }
        $this->delaySeconds = $delaySeconds;
        return $this;
    }

    /**
     * @return int
     */
    public function getDelay(): int
    {
        return $this->delaySeconds;
    }

    /**
     * @param \DateInterval $retriesTimeLimit {@link $retriesTimeLimit}
     */
    public function setRetriesTimeLimit(\DateInterval $retriesTimeLimit): self
    {
        $this->retriesTimeLimit = $retriesTimeLimit;
        return $this;
    }

    /**
     * @return \DateInterval
     */
    public function getRetriesTimeLimit(): \DateInterval
    {
        return $this->retriesTimeLimit;
    }

    /**
     * @param callable(int, int, ResponseInterface):bool $shouldRetry {@link $shouldRetry}
     */
    public function setShouldRetry(callable $shouldRetry): self
    {
        $this->shouldRetry = $shouldRetry;
        return $this;
    }

    /**
     * @return callable(int, int, ResponseInterface):bool
     */
    public function getShouldRetry(): callable
    {
        return $this->shouldRetry;
    }
}
