<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http\Middleware\Options;

use GuzzleHttp\Psr7\Utils;
use Microsoft\Kiota\Abstractions\RequestOption;
use Psr\Http\Message\RequestInterface;

/**
 * Class CompressionOption
 *
 * Configurations for a {@link CompressionHandler}
 *
 * @package Microsoft\Kiota\Http\Middleware\Options
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class CompressionOption implements RequestOption
{
    /**
     * @var callable[] compression algorithms to be applied in order of occurrence.
     * Callable should take in a RequestInterface, compress the request body, set the Content-Encoding header and return the RequestInterface object
     * callable(Psr\Http\Message\RequestInterface):Psr\Http\Message\RequestInterface
     */
    private array $compressionCallbacks;

    /**
     * Uses GZIP by default
     *
     * @param callable[] $compressionCallbacks {@link $compressionCallbacks}
     */
    public function __construct(array $compressionCallbacks = [])
    {
        if (empty($compressionCallbacks)) {
            $compressionCallbacks[] = CompressionOption::gzip();
        }
        $this->compressionCallbacks = $compressionCallbacks;
    }

    /**
     * @param callable[] $callbacks {@link $compressionCallbacks}
     */
    public function setCallbacks(array $callbacks) {
        $this->compressionCallbacks = $callbacks;
    }

    /**
     * @return callable[] {@link $compressionCallbacks}
     */
    public function getCallbacks(): array {
        return $this->compressionCallbacks;
    }

    /**
     * Returns callback that applies GZIP compression to a RequestInterface body & adds the appropriate Content-Encoding header
     *
     * @return callable(RequestInterface):RequestInterface
     */
    public static function gzip(): callable
    {
        return static function (RequestInterface $request): RequestInterface {
            // Check if the request has a body
            if ($request->getBody()->getSize()) {
                $compressedBody = gzencode($request->getBody()->getContents());
                return $request->withBody(Utils::streamFor($compressedBody))
                                ->withAddedHeader('Content-Encoding', 'gzip');
            }
            return $request;
        };
    }

    /**
     * Returns callback that applies DEFLATE compression to a RequestInterface body & adds the appropriate
     * Content-Encoding header
     *
     * @return callable(RequestInterface):RequestInterface
     */
    public static function deflate(): callable
    {
        return static function (RequestInterface $request): RequestInterface {
            // Check if the request has a body
            if ($request->getBody()->getSize()) {
                $compressedBody = gzdeflate($request->getBody()->getContents());
                return $request->withBody(Utils::streamFor($compressedBody))
                                ->withAddedHeader('Content-Encoding', 'deflate');
            }
            return $request;
        };
    }
}
