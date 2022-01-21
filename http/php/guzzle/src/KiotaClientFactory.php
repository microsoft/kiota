<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http;


use GuzzleHttp\Client;
use GuzzleHttp\HandlerStack;
use GuzzleHttp\Utils;
use Microsoft\Kiota\Http\Middleware\KiotaMiddleware;

/**
 * Class KiotaClientFactory
 *
 * This class is used to build the \GuzzleHttp\Client instance used by the core service.
 *
 * @package Microsoft\Kiota\Http
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class KiotaClientFactory
{
    /**
     * Initialises the Guzzle client with default middleware
     *
     * @return Client
     */
    public static function create(): \GuzzleHttp\Client
    {
        return self::createWithMiddleware(self::getDefaultHandlerStack());
    }

    /**
     * Initialises the Guzzle client with provided middleware
     *
     * @param HandlerStack $handlerStack
     * @return Client
     */
    public static function createWithMiddleware(HandlerStack $handlerStack): \GuzzleHttp\Client
    {
        return new Client(['handler' => $handlerStack]);
    }

    /**
     * Initialises the client with Guzzle request options (https://docs.guzzlephp.org/en/stable/request-options.html)
     *
     * @param array $guzzleConfig
     * @return Client
     */
    public static function createWithConfig(array $guzzleConfig): \GuzzleHttp\Client
    {
        return new Client($guzzleConfig);
    }

    /**
     * Returns default set of middleware to use for Guzzle clients
     *
     * @return HandlerStack
     */
    public static function getDefaultHandlerStack(): HandlerStack
    {
        $handlerStack = new HandlerStack(Utils::chooseHandler());
        $handlerStack->push(KiotaMiddleware::retry());
        $handlerStack->push(\GuzzleHttp\Middleware::redirect());
        return $handlerStack;
    }
}
