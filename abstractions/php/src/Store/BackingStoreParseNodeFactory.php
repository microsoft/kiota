<?php

namespace Microsoft\Kiota\Abstractions\Store;


use Microsoft\Kiota\Abstractions\Serialization\ParseNodeFactory;
use Microsoft\Kiota\Abstractions\Serialization\ParseNodeProxyFactory;

class BackingStoreParseNodeFactory extends ParseNodeProxyFactory{

    /**
     * BackingStoreParseNodeFactory constructor.
     * @param ParseNodeFactory $concrete
     */
    public function __construct(ParseNodeFactory $concrete) {
        parent::__construct($concrete,
           function ($x) {
             if (is_a($x, BackedModel::class)) {
                 $backedModel = $x;
                 $backingStore = $backedModel->getBackingStore();

                 if (!is_null($backingStore)) {
                     $backingStore->setIsInitializationCompleted(false);
                 }
             }
           },
           function ($x) {
               if (is_a($x, BackedModel::class)) {
                   $backedModel = $x;
                   $backingStore = $backedModel->getBackingStore();

                   if (!is_null($backingStore)) {
                       $backingStore->setIsInitializationCompleted(true);
                   }
               }
           }
        );
    }

}
