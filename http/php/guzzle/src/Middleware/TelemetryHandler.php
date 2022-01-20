<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware;

use GuzzleHttp\Promise\PromiseInterface;
use Microsoft\Kiota\Http\Middleware\Options\TelemetryOption;
use Psr\Http\Message\RequestInterface;

/**
 * Class TelemetryHandler
 *
 * Middleware that allows configuration of a RequestInterface with telemetry data
 *
 * @package Microsoft\Kiota\Http\Middleware
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class TelemetryHandler
{
    /**
     * @var callable(RequestInterface, array):PromiseInterface
     * Next handler in the handler stack
     */
    private $nextHandler;

    /**
     * @var TelemetryOption|null {@link TelemetryOption}
     */
    private ?TelemetryOption $telemetryOption;

    public function __construct(callable $nextHandler, ?TelemetryOption $telemetryOption = null)
    {
        $this->nextHandler = $nextHandler;
        $this->telemetryOption = $telemetryOption;
    }

    public function __invoke(RequestInterface $request, array $options): PromiseInterface
    {
        if ($this->telemetryOption && $this->telemetryOption->getTelemetryConfigurator()) {
            $request = $this->telemetryOption->getTelemetryConfigurator()($request);
        }
        $fn = $this->nextHandler;
        return $fn($request, $options);
    }
}
