<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware\Options;

use Microsoft\Kiota\Abstractions\RequestOption;
use Microsoft\Kiota\Http\Middleware\ParametersNameDecodingHandler;

/**
 * Class ParametersDecodingOption
 *
 * Config options for the {@link ParametersNameDecodingHandler}
 *
 * @package Microsoft\Kiota\Http\Middleware\Options
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class ParametersDecodingOption implements RequestOption
{
    /**
     * @var bool Whether to decode characters in the request query parameter names
     */
    private bool $enabled;
    /**
     * @var array|string[] list of characters to decode in the request query parameter names before executing request
     */
    private array $parametersToDecode;

    /**
     * @param array|string[] $parametersToDecode {@link $parameterstoDecode}
     * @param bool $enabled {@link $enabled}
     */
    public function __construct(array $parametersToDecode = ['.', '-', '~', '$'], bool $enabled = true)
    {
        $this->parametersToDecode = $parametersToDecode;
        $this->enabled = $enabled;
    }

    /**
     * @return bool
     */
    public function isEnabled(): bool
    {
        return $this->enabled;
    }

    /**
     * @param bool $enabled
     */
    public function setEnabled(bool $enabled): void
    {
        $this->enabled = $enabled;
    }

    /**
     * @return array|string[]
     */
    public function getParametersToDecode(): array
    {
        return $this->parametersToDecode;
    }

    /**
     * @param array|string[] $parametersToDecode
     */
    public function setParametersToDecode(array $parametersToDecode): void
    {
        $this->parametersToDecode = $parametersToDecode;
    }
}
