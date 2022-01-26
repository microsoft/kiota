<?php


namespace Microsoft\Kiota\Abstractions\Store;

/** Defines the contracts for a model that is backed by a store. */
interface BackedModel {
    /**
     * Gets the store that is backing the model.
     * @return BackingStore|null the backing store.
     */
    public function getBackingStore(): ?BackingStore;
}
