<?php
namespace Microsoft\Kiota\Abstractions;

use Http\Promise\Promise;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactory;
use Microsoft\Kiota\Abstractions\Store\BackingStoreFactory;

/** Service responsible for translating abstract Request Info into concrete native HTTP requests. */
interface RequestAdapter {
    /**
     * Executes the HTTP request specified by the given RequestInformation and returns the deserialized response model.
     * @param RequestInformation $requestInfo the request info to execute.
     * @param string $targetClass the class of the response model to deserialize the response into.
     * @param ResponseHandler|null $responseHandler The response handler to use for the HTTP request instead of the default handler.
     * @return Promise with the deserialized response model.
     */
    public function sendAsync(RequestInformation $requestInfo, string $targetClass, ?ResponseHandler $responseHandler = null): Promise;

    /**
     * Gets the serialization writer factory currently in use for the HTTP core service.
     * @return SerializationWriterFactory the serialization writer factory currently in use for the HTTP core service.
     */
    public function getSerializationWriterFactory(): SerializationWriterFactory;

    /**
     * Executes the HTTP request specified by the given RequestInformation and returns the deserialized response model collection.
     * @param RequestInformation $requestInfo
     * @param string $targetClass the request info to execute.
     * @param ResponseHandler|null $responseHandler
     * @return Promise with the deserialized response model collection.
     */
    public function sendCollectionAsync(RequestInformation $requestInfo, string $targetClass, ?ResponseHandler $responseHandler = null): Promise;

    /**
     * Executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model.
     * @param RequestInformation $requestInfo
     * @param string $primitiveType e.g. int, bool
     * @param ResponseHandler|null $responseHandler
     * @return Promise
     */
    public function sendPrimitiveAsync(RequestInformation $requestInfo, string $primitiveType, ?ResponseHandler $responseHandler = null): Promise;

    /**
     * Executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model collection.
     * @param RequestInformation $requestInfo
     * @param string $primitiveType e.g. int, bool
     * @param ResponseHandler|null $responseHandler
     * @return Promise
     */
    public function sendPrimitiveCollectionAsync(RequestInformation $requestInfo, string $primitiveType, ?ResponseHandler $responseHandler = null): Promise;

    /**
     * Executes the HTTP request specified by the given RequestInformation with no return content.
     * @param RequestInformation $requestInfo
     * @param ResponseHandler|null $responseHandler
     * @return Promise
     */
    public function sendNoContentAsync(RequestInformation $requestInfo, ?ResponseHandler $responseHandler = null): Promise;
    /**
     * Enables the backing store proxies for the SerializationWriters and ParseNodes in use.
     * @param BackingStoreFactory $backingStoreFactory The backing store factory to use.
     */
    public function enableBackingStore(BackingStoreFactory $backingStoreFactory): void;

    /**
     * Sets The base url for every request.
     * @param string $baseUrl The base url for every request.
     */
    public function setBaseUrl(string $baseUrl): void;

    /**
     * Gets The base url for every request.
     * @return string The base url for every request.
     */
    public function getBaseUrl(): string;
}
