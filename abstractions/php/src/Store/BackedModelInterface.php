<?php


namespace Microsoft\Kiota\Abstractions\Store;

interface BackedModelInterface {
    /**
     * @return BackingStoreInterface|null
     */
    public function getBackingStore(): ?BackingStoreInterface;
}
