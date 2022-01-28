<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware;

use Microsoft\Kiota\Http\Middleware\Options\ChaosOption;
use Microsoft\Kiota\Http\Middleware\Options\CompressionOption;
use Microsoft\Kiota\Http\Middleware\Options\RetryOption;
use Microsoft\Kiota\Http\Middleware\Options\TelemetryOption;

/**
 * Class KiotaMiddleware
 *
 * Utility methods to expose middleware components and easily add them to Guzzle's HandlerStack
 *
 * @package Microsoft\Kiota\Http\Middleware
 * @copyright 2021 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class KiotaMiddleware
{
    /**
     * Middleware that retries requests for 429,503 and 504 response status codes (by default) while respecting the Retry-After response header
     * Configurable using {@link RetryOption}
     *
     * @param RetryOption|null $retryOption
     * @return callable
     */
    public static function retry(?RetryOption $retryOption = null): callable
    {
        return static function (callable $handler) use ($retryOption) : RetryHandler {
            return new RetryHandler($handler, $retryOption);
        };
    }

    /**
     * Middleware that compresses a request body based on compression callbacks provided in {@link CompressionOption} and retries
     * the initial request with an uncompressed body only once if a 415 response is received.
     *
     * @param CompressionOption|null $compressionOption
     * @return callable
     */
    public static function compression(?CompressionOption $compressionOption = null): callable
    {
        return static function (callable $handler) use ($compressionOption): CompressionHandler {
            return new CompressionHandler($handler, $compressionOption);
        };
    }

    /**
     * Middleware that allows configuration of a RequestInterface with telemetry data
     *
     * @param TelemetryOption|null $telemetryOption
     * @return callable
     */
    public static function telemetry(?TelemetryOption $telemetryOption = null): callable
    {
        return static function (callable $handler) use ($telemetryOption): TelemetryHandler {
            return new TelemetryHandler($handler, $telemetryOption);
        };
    }

    /**
     * Middleware that selects a chaos response (configured via {@link ChaosOption}) at random x% of the time
     * If criteria is not met for a chaos response, the request is forwarded down the middleware chain
     * @param ChaosOption|null $chaosOption
     * @return callable
     */
    public static function chaos(?ChaosOption $chaosOption = null): callable
    {
        return static function (callable $handler) use ($chaosOption): ChaosHandler {
            return new ChaosHandler($handler, $chaosOption);
        };
    }
}
