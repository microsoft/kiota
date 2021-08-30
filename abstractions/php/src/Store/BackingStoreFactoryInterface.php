<?php

namespace Microsoft\Kiota\Abstractions\Store;

interface BackingStoreFactoryInterface
{
    /**
     * @return BackingStoreInterface
     */
    public function createBackingStore(): BackingStoreInterface;
}