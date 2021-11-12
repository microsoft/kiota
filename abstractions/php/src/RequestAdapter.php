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
     * @param mixed|null $targetClass the class of the response model to deserialize the response into.
     * @param ResponseHandler|null $responseHandler The response handler to use for the HTTP request instead of the default handler.
     * @return Promise with the deserialized response model.
     */
    public function sendAsync(RequestInformation $requestInfo, $targetClass = null, ?ResponseHandler $responseHandler = null): Promise;

    /**
     * Gets the serialization writer factory currently in use for the HTTP core service.
     * @return SerializationWriterFactory the serialization writer factory currently in use for the HTTP core service.
     */
    public function getSerializationWriterFactory(): SerializationWriterFactory;

    /**
     * Executes the HTTP request specified by the given RequestInformation and returns the deserialized response model collection.
     * @param RequestInformation $requestInfo
     * @param mixed $targetClass the request info to execute.
     * @param ResponseHandler|null $responseHandler
     * @return Promise with the deserialized response model collection.
     */
    public function sendCollectionAsync(RequestInformation $requestInfo, $targetClass = null, ?ResponseHandler $responseHandler = null): Promise;

    /**
     * Enables the backing store proxies for the SerializationWriters and ParseNodes in use.
     * @param BackingStoreFactory $backingStoreFactory The backing store factory to use.
     */
    public function enableBackingStore(BackingStoreFactory $backingStoreFactory): void;
}
