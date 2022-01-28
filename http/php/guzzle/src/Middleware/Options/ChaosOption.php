<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware\Options;

use Microsoft\Kiota\Abstractions\RequestOption;
use Microsoft\Kiota\Http\Middleware\ChaosHandler;

/**
 * Class ChaosOption
 *
 * Configs for {@link ChaosHandler}
 *
 * @package Microsoft\Kiota\Http\Middleware\Options
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class ChaosOption implements RequestOption
{
    public const MAX_CHAOS_PERCENTAGE = 100;

    /**
     * @var int Threshold below which a random $chaosResponse is returned
     */
    private int $chaosPercentage;

    /**
     * @var array set of chaos ResponseInterfaces or callables(RequestInterface, array):ResponseInterface that are returned/executed at random.
     */
    private array $chaosResponses = [];

    /**
     * @param int $chaosPercentage
     * @param array $chaosResponses
     */
    public function __construct(array $chaosResponses = [], int $chaosPercentage = 10)
    {
        if ($chaosPercentage < 0 || $chaosPercentage > self::MAX_CHAOS_PERCENTAGE) {
            throw new \InvalidArgumentException("Chaos percentage should be between 0 and ".self::MAX_CHAOS_PERCENTAGE);
        }

        $this->chaosPercentage = $chaosPercentage;
        $this->chaosResponses = $chaosResponses;
    }

    /**
     * @param int $chaosPercentage
     */
    public function setChaosPercentage(int $chaosPercentage): void
    {
        if ($chaosPercentage < 0 || $chaosPercentage > self::MAX_CHAOS_PERCENTAGE) {
            throw new \InvalidArgumentException("Chaos percentage should be between 0 and ".self::MAX_CHAOS_PERCENTAGE);
        }
        $this->chaosPercentage = $chaosPercentage;
    }

    /**
     * @return int
     */
    public function getChaosPercentage(): int
    {
        return $this->chaosPercentage;
    }

    /**
     * @param array $chaosResponses
     */
    public function setChaosResponses(array $chaosResponses): void
    {
        $this->chaosResponses = $chaosResponses;
    }

    /**
     * @return array
     */
    public function getChaosResponses(): array
    {
        return $this->chaosResponses;
    }
}
