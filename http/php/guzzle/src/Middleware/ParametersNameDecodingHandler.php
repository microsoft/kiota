<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware;


use GuzzleHttp\Promise\PromiseInterface;
use GuzzleHttp\Psr7\Uri;
use Microsoft\Kiota\Http\Middleware\Options\ParametersDecodingOption;
use Psr\Http\Message\RequestInterface;

/**
 * Class ParametersNameDecodingHandler
 *
 * This handler decodes special characters in the request query parameter names that had to be encoded due to RFC 6570
 * restrictions before executing the request.
 *
 * @package Microsoft\Kiota\Http\Middleware
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class ParametersNameDecodingHandler
{
    /**
     * @var ParametersDecodingOption configuration for the middleware
     */
    private ParametersDecodingOption $decodingOption;
    /**
     * @var callable(RequestInterface, array): PromiseInterface
     */
    private $nextHandler;

    /**
     * @param callable $nextHandler
     * @param ParametersDecodingOption|null $decodingOption
     */
    public function __construct(callable $nextHandler, ?ParametersDecodingOption $decodingOption = null)
    {
        $this->nextHandler = $nextHandler;
        $this->decodingOption = ($decodingOption) ?: new ParametersDecodingOption();
    }

    /**
     * @param RequestInterface $request
     * @param array $options
     * @return PromiseInterface
     */
    public function __invoke(RequestInterface $request, array $options): PromiseInterface
    {
        // Request-level options override global options
        if (array_key_exists(ParametersDecodingOption::class, $options)) {
            $this->decodingOption = $options[ParametersDecodingOption::class];
        }
        $request = $this->decodeQueryParameters($request);
        $fn = $this->nextHandler;
        return $fn($request, $options);
    }

    /**
     * @param RequestInterface $request
     * @return RequestInterface
     */
    private function decodeQueryParameters(RequestInterface $request): RequestInterface
    {
        if (!$this->decodingOption->isEnabled() || !$this->decodingOption->getParametersToDecode()) {
            return $request;
        }
        $encodingsToReplace = array_map(function ($character) { return "%".dechex(ord($character)); }, $this->decodingOption->getParametersToDecode());
        $decodedUri = str_ireplace($encodingsToReplace, $this->decodingOption->getParametersToDecode(), $request->getUri());
        return $request->withUri(new Uri($decodedUri));
    }
}
