<?php

namespace Microsoft\Kiota\Abstractions\Store;

/** Defines the contract for a factory that creates backing stores. */
interface BackingStoreFactory
{
    /**
     * Creates a new instance of the backing store.
     * @return BackingStore a new instance of the backing store.
     */
    public function createBackingStore(): BackingStore;
}