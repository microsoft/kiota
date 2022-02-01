<?php

namespace Microsoft\Kiota\Abstractions\Store;

/** This class is used to create instances of InMemoryBackingStore */
class InMemoryBackingStoreFactory implements BackingStoreFactory
{
    /**
     * @return BackingStore
     */
    public function createBackingStore(): BackingStore {
        return new InMemoryBackingStore();
    }
}