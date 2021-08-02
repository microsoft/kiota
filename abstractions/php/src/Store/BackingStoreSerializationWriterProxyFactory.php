<?php

namespace Microsoft\Kiota\Abstractions\Store;

use Closure;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactoryInterface;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterProxyFactory;

class BackingStoreSerializationWriterProxyFactory extends SerializationWriterProxyFactory {
    public function __construct(SerializationWriterFactoryInterface $concrete){
        parent::__construct($concrete,
            function ($x) {

            },
            function ($x) {
              if (is_a($x, BackedModelInterface::class)) {
                  $backedModel = $x;

              }
            }
        );
    }
}