<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware;

use Microsoft\Kiota\Http\Middleware\Options\RetryOption;

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
    public static function retry(?RetryOption $retryOption): callable
    {
        return static function (callable $handler) use ($retryOption) : RetryHandler {
            return new RetryHandler($retryOption, $handler);
        };
    }
}
