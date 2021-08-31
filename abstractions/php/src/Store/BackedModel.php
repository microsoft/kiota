<?php


namespace Microsoft\Kiota\Abstractions\Store;

interface BackedModel {
    /**
     * @return BackingStore|null
     */
    public function getBackingStore(): ?BackingStore;
}
