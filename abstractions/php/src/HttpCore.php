<?php
namespace Microsoft\Kiota\Abstractions;

use Http\Promise\Promise;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactory;
use Microsoft\Kiota\Abstractions\Store\BackingStoreFactory;

interface HttpCore {
    /**
     * @param RequestInfo $requestInfo
     * @param object|mixed $targetClass
     * @param ResponseHandler $responseHandler
     * @return Promise
     */
    public function sendAsync(RequestInfo $requestInfo, $targetClass, ResponseHandler $responseHandler): Promise;

    /**
     * @return SerializationWriterFactory
     */
    public function getSerializationWriterFactory(): SerializationWriterFactory;

    /**
     * @param RequestInfo $requestInfo
     * @param mixed|object $targetClass
     * @param ResponseHandler $responseHandler
     * @return Promise
     */
    public function sendCollectionAsync(RequestInfo $requestInfo, $targetClass, ResponseHandler $responseHandler): Promise;

    /**
     * @param BackingStoreFactory $backingStoreFactory
     */
    public function enableBackingStore(BackingStoreFactory $backingStoreFactory): void;
}
