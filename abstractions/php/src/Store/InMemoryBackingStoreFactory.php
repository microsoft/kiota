<?php

namespace Microsoft\Kiota\Abstractions\Store;

class InMemoryBackingStoreFactory implements BackingStoreFactoryInterface
{
    /**
     * @return BackingStoreInterface
     */
    public function createBackingStore(): BackingStoreInterface {
        return new InMemoryBackingStore();
    }
}