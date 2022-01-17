<?php

namespace Microsoft\Kiota\Abstractions\Store;

use Microsoft\Kiota\Abstractions\Serialization\Parsable;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactory;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterProxyFactory;

/**Proxy implementation of SerializationWriterFactory for the backing store that automatically sets the state of the backing store when serializing. */

class BackingStoreSerializationWriterProxyFactory extends SerializationWriterProxyFactory {

    /**
     * Initializes a new instance of the BackingStoreSerializationWriterProxyFactory class given a concrete implementation of SerializationWriterFactory.
     * @param SerializationWriterFactory $concreteSerializationWriterFactory a concrete implementation of SerializationWriterFactory to wrap.
     */
    public function __construct(SerializationWriterFactory $concreteSerializationWriterFactory){
        $onBeforeObjectSerialization = static function (Parsable $model) {
            if (is_a($model, BackedModel::class)) {
                $backedModel = $model;
                $backingStore = $backedModel->getBackingStore();
                if ($backingStore !== null) {
                    $backingStore->setReturnOnlyChangedValues(true);
                }
            }
        };

        $onAfterObjectSerialization = static function (Parsable $model) {
            if (is_a($model, BackedModel::class)) {
                $backedModel = $model;
                $backingStore = $backedModel->getBackingStore();

                if ($backingStore !== null) {
                    $backingStore->setReturnOnlyChangedValues(false);
                    $backingStore->setIsInitializationCompleted(true);
                }
            }
        };

        $onStartObjectSerialization = static function (Parsable $model, SerializationWriter $serializationWriter) {
            if (is_a($model, BackedModel::class)) {
                $backedModel = $model;

                $backingStore = $backedModel->getBackingStore();

                if ($backingStore !== null) {
                    $keys = $backingStore->enumerateKeysForValuesChangedToNull();

                    foreach ($keys as $key) {
                        $serializationWriter->writeNullValue($key);
                    }
                }
            }
        };
        parent::__construct($concreteSerializationWriterFactory, $onBeforeObjectSerialization,
            $onAfterObjectSerialization, $onStartObjectSerialization);
    }
}