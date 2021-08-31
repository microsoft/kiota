<?php

namespace Microsoft\Kiota\Abstractions\Store;

use Closure;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactory;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterProxyFactory;

class BackingStoreSerializationWriterProxyFactory extends SerializationWriterProxyFactory {
    public function __construct(SerializationWriterFactory $concrete){
        parent::__construct($concrete,
            function ($x) {

            },
            function ($x) {
              if (is_a($x, BackedModel::class)) {
                  $backedModel = $x;

              }
            }
        );
    }
}