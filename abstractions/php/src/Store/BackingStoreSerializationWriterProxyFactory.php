<?php

namespace Microsoft\Kiota\Abstractions\Store;

use Closure;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactory;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterProxyFactory;

class BackingStoreSerializationWriterProxyFactory extends SerializationWriterProxyFactory {
    public function __construct(SerializationWriterFactory $concrete){
        parent::__construct($concrete,
            function ($x) {
                if (is_a($x, BackedModel::class)) {
                    $backedModel = $x;
                    $backingStore = $backedModel->getBackingStore();
                    if ($backingStore !== null) {
                        $backingStore->setReturnOnlyChangedValues(true);
                    }
                }
            },
            function ($x) {
                if (is_a($x, BackedModel::class)) {
                    $backedModel = $x;
                    $backingStore = $backedModel->getBackingStore();

                    $backingStore->setReturnOnlyChangedValues(false);
                    $backingStore->setIsInitializationCompleted(true);
                }
            },
            function ($x, $y) {
                if (is_a($x, BackedModel::class)) {
                    $backedModel = $x;

                    $backingStore = $backedModel->getBackingStore();

                    if ($backingStore !== null) {
                        $keys = $backingStore->enumerateKeysForValuesChangedToNull();

                        foreach ($keys as $key) {
                            $y->writeNullValue($key);
                        }
                    }
                }
            }
        );
    }
}