<?php

namespace Microsoft\Kiota\Abstractions\Store;

class InMemoryBackingStoreFactory implements BackingStoreFactory
{
    /**
     * @return BackingStore
     */
    public function createBackingStore(): BackingStore {
        return new InMemoryBackingStore();
    }
}