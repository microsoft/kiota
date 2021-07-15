<?php

namespace Microsoft\Kiota\Abstractions\Store;


use Microsoft\Kiota\Abstractions\Serialization\ParseNodeFactoryInterface;
use Microsoft\Kiota\Abstractions\Serialization\AbstractParseNodeProxyFactory;

class BackingStoreAbstractParseNodeFactory extends AbstractParseNodeProxyFactory{

    /**
     * BackingStoreParseNodeFactory constructor.
     * @param ParseNodeFactoryInterface $concrete
     */
    public function __construct(ParseNodeFactoryInterface $concrete) {
        parent::__construct($concrete,
           function ($x) {
             if (is_a($x, BackedModelInterface::class)) {
                 $backedModel = $x;
                 $backingStore = $backedModel->getBackingStore();

                 if (!is_null($backingStore)) {
                     $backingStore->setIsInitializationCompleted(false);
                 }
             }
           },
           function ($x) {
               if (is_a($x, BackedModelInterface::class)) {
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
