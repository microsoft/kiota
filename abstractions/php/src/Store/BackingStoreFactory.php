<?php

namespace Microsoft\Kiota\Abstractions\Store;

interface BackingStoreFactory
{
    /**
     * @return BackingStore
     */
    public function createBackingStore(): BackingStore;
}