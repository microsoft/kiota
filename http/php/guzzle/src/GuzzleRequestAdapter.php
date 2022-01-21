<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Http;


use GuzzleHttp\Psr7\Request;
use Http\Adapter\Guzzle7\Client as GuzzleClientAdapter;
use Http\Promise\Promise;
use Microsoft\Kiota\Abstractions\Authentication\AuthenticationProvider;
use Microsoft\Kiota\Abstractions\RequestAdapter;
use Microsoft\Kiota\Abstractions\RequestInformation;
use Microsoft\Kiota\Abstractions\ResponseHandler;
use Microsoft\Kiota\Abstractions\Serialization\ParseNode;
use Microsoft\Kiota\Abstractions\Serialization\ParseNodeFactory;
use Microsoft\Kiota\Abstractions\Serialization\ParseNodeFactoryRegistry;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactory;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactoryRegistry;
use Microsoft\Kiota\Abstractions\Store\BackingStoreFactory;
use Psr\Http\Message\RequestInterface;
use Psr\Http\Message\ResponseInterface;
use Psr\Http\Message\StreamInterface;

/**
 * Class GuzzleRequestAdapter
 * @package Microsoft\Kiota\Http
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
class GuzzleRequestAdapter implements RequestAdapter
{
    /**
     * @var GuzzleClientAdapter PHP-HTTP wrapper around \GuzzleHttp\Guzzle clients to make async requests
     */
    private GuzzleClientAdapter $guzzleAdapter;

    /**
     * @var AuthenticationProvider
     */
    private AuthenticationProvider $authenticationProvider;

    /**
     * @var ParseNodeFactory|ParseNodeFactoryRegistry
     */
    private ParseNodeFactory $parseNodeFactory;

    /**
     * @var SerializationWriterFactory|SerializationWriterFactoryRegistry
     */
    private SerializationWriterFactory $serializationWriterFactory;

    private string $baseUrl;

    /**
     * @param AuthenticationProvider $authenticationProvider
     * @param ParseNodeFactory|null $parseNodeFactory
     * @param SerializationWriterFactory|null $serializationWriterFactory
     * @param \GuzzleHttp\Client|null $guzzleClient
     */
    public function __construct(AuthenticationProvider $authenticationProvider,
                                ?ParseNodeFactory $parseNodeFactory = null,
                                ?SerializationWriterFactory $serializationWriterFactory = null,
                                ?\GuzzleHttp\Client $guzzleClient = null)
    {
        $this->authenticationProvider = $authenticationProvider;
        $this->parseNodeFactory = ($parseNodeFactory) ?: ParseNodeFactoryRegistry::getDefaultInstance();
        $this->serializationWriterFactory = ($serializationWriterFactory) ?: SerializationWriterFactoryRegistry::getDefaultInstance();
        $this->guzzleAdapter = ($guzzleClient) ? new GuzzleClientAdapter($guzzleClient) : new GuzzleClientAdapter(KiotaClientFactory::create());
    }

    /**
     * @inheritDoc
     */
    public function sendAsync(RequestInformation $requestInfo, string $targetClass, ?ResponseHandler $responseHandler = null): Promise
    {
        $response = $this->getHttpResponseMessage($requestInfo);
        return $response->then(
            function (ResponseInterface $result) use ($targetClass, $responseHandler) {
                if (!$responseHandler) {
                    $rootNode = $this->getRootParseNode($result);
                    if ($targetClass === StreamInterface::class || in_array(StreamInterface::class, class_implements($targetClass))) {
                        return $result->getBody();
                    }
                    return $rootNode->getObjectValue($targetClass);
                }
                return $responseHandler->handleResponseAsync($result);
            }
        );
    }

    /**
     * @inheritDoc
     */
    public function getSerializationWriterFactory(): SerializationWriterFactory
    {
        return $this->serializationWriterFactory;
    }

    /**
     * @inheritDoc
     */
    public function sendCollectionAsync(RequestInformation $requestInfo, string $targetClass, ?ResponseHandler $responseHandler = null): Promise
    {
        $response = $this->getHttpResponseMessage($requestInfo);
        return $response->then(
            function (ResponseInterface $result) use ($targetClass, $responseHandler) {
                if (!$responseHandler) {
                    $rootNode = $this->getRootParseNode($result);
                    return $rootNode->getCollectionOfObjectValues($targetClass);
                }
                return $responseHandler->handleResponseAsync($result);
            }
        );
    }

    /**
     * @inheritDoc
     */
    public function sendPrimitiveAsync(RequestInformation $requestInfo, string $primitiveType, ?ResponseHandler $responseHandler = null): Promise
    {
        $response = $this->getHttpResponseMessage($requestInfo);
        return $response->then(
            function (ResponseInterface $result) use ($primitiveType, $responseHandler) {
                if (!$responseHandler) {
                    $rootParseNode = $this->getRootParseNode($result);
                    switch (strtolower($primitiveType)) {
                        case 'int':
                        case 'long':
                            return $rootParseNode->getIntegerValue();
                        case 'float':
                            return $rootParseNode->getFloatValue();
                        case 'bool':
                            return $rootParseNode->getBooleanValue();
                        case 'string':
                            return $rootParseNode->getStringValue();
                        default:
                            throw new \InvalidArgumentException("Unsupported primitive type $primitiveType");
                    }
                }
                return $responseHandler->handleResponseAsync($result);
            }
        );
    }

    /**
     * @inheritDoc
     */
    public function sendPrimitiveCollectionAsync(RequestInformation $requestInfo, string $primitiveType, ?ResponseHandler $responseHandler = null): Promise
    {
        $response = $this->getHttpResponseMessage($requestInfo);
        return $response->then(
            function (ResponseInterface $result) use ($primitiveType, $responseHandler) {
                if (!$responseHandler) {
                    $rootParseNode = $this->getRootParseNode($result);
                    return $rootParseNode->getCollectionOfPrimitiveValues($primitiveType);
                }
                return $responseHandler->handleResponseAsync($result);
            }
        );
    }

    /**
     * @inheritDoc
     */
    public function sendNoContentAsync(RequestInformation $requestInfo, ?ResponseHandler $responseHandler = null): Promise
    {
        $response = $this->getHttpResponseMessage($requestInfo);
        return $response->then(
            function (ResponseInterface $result) use ($responseHandler) {
                if ($responseHandler) {
                    $responseHandler->handleResponseAsync($result);
                }
            }
        );
    }

    /**
     * @inheritDoc
     */
    public function enableBackingStore(BackingStoreFactory $backingStoreFactory): void
    {
        // TODO: Implement enableBackingStore() method.
    }

    /**
     * @inheritDoc
     */
    public function setBaseUrl(string $baseUrl): void
    {
        $this->baseUrl = $baseUrl;
    }

    /**
     * @inheritDoc
     */
    public function getBaseUrl(): string
    {
        return $this->baseUrl;
    }

    /**
     * Create and returns a PSR 7 Request object from {@link RequestInformation}
     *
     * @param RequestInformation $requestInformation
     * @return RequestInterface
     */
    public function getPsrRequestFromRequestInformation(RequestInformation $requestInformation): RequestInterface
    {
        $requestInformation->pathParameters["baseurl"] = $this->getBaseUrl();
        return new Request(
            $requestInformation->httpMethod,
            $requestInformation->getUri(),
            $requestInformation->headers,
            $requestInformation->content
        );
    }

    private function getRootParseNode(ResponseInterface $response): ParseNode
    {
        if (!$response->hasHeader(RequestInformation::$contentTypeHeader)) {
            throw new \RuntimeException("No response content type header for deserialization");
        }
        $contentType = $response->getHeaderLine(RequestInformation::$contentTypeHeader);
        return $this->parseNodeFactory->getRootParseNode($contentType, $response->getBody());
    }

    private function getHttpResponseMessage(RequestInformation $requestInformation): Promise
    {
        $request = $this->authenticationProvider->authenticateRequest($requestInformation);
        return $request->then(
            function (RequestInformation $requestInformation) {
                $psrRequest = $this->getPsrRequestFromRequestInformation($requestInformation);
                //TODO: Pass request options to the client & middleware downstream
                return $this->guzzleAdapter->sendRequest($psrRequest);
            }
        );
    }
}
