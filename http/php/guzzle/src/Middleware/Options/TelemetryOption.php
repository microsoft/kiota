<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware\Options;

use Microsoft\Kiota\Abstractions\RequestOption;
use Microsoft\Kiota\Http\Middleware\TelemetryHandler;
use Psr\Http\Message\RequestInterface;

/**
 * Class TelemetryOption
 *
 * Configs for a {@link TelemetryHandler}
 *
 * @package Microsoft\Kiota\Http\Middleware\Options
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class TelemetryOption implements RequestOption
{
    /**
     * @var callable(RequestInterface):RequestInterface
     * Function that adds appropriate telemetry information to a request
     */
    private $telemetryConfigurator;

    /**
     * @param callable(RequestInterface): RequestInterface|null $telemetryConfigurator {@link $telemetryConfigurator}
     */
    public function __construct(?callable $telemetryConfigurator = null)
    {
        $this->telemetryConfigurator = $telemetryConfigurator;
    }

    /**
     * @return callable
     */
    public function getTelemetryConfigurator(): ?callable
    {
        return $this->telemetryConfigurator;
    }

    /**
     * @param callable(RequestInterface):RequestInterface|null $telemetryConfigurator
     */
    public function setTelemetryConfigurator(?callable $telemetryConfigurator): void
    {
        $this->telemetryConfigurator = $telemetryConfigurator;
    }
}
