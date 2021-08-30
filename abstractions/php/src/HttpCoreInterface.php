<?php
namespace Microsoft\Kiota\Abstractions;

use Http\Promise\Promise;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactoryInterface;
use Microsoft\Kiota\Abstractions\Store\BackingStoreFactoryInterface;

interface HttpCoreInterface {
    /**
     * @param RequestInfo $requestInfo
     * @param object|mixed $targetClass
     * @param AbstractResponseHandler $responseHandler
     * @return Promise
     */
    public function sendAsync(RequestInfo $requestInfo, $targetClass, AbstractResponseHandler $responseHandler): Promise;

    /**
     * @return SerializationWriterFactoryInterface
     */
    public function getSerializationWriterFactory(): SerializationWriterFactoryInterface;

    /**
     * @param RequestInfo $requestInfo
     * @param mixed|object $targetClass
     * @param AbstractResponseHandler $responseHandler
     * @return Promise
     */
    public function sendCollectionAsync(RequestInfo $requestInfo, $targetClass, AbstractResponseHandler $responseHandler): Promise;

    /**
     * @param BackingStoreFactoryInterface $backingStoreFactory
     */
    public function enableBackingStore(BackingStoreFactoryInterface $backingStoreFactory): void;
}
