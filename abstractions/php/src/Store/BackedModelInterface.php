<?php


namespace Microsoft\Kiota\Abstractions\Store;

interface BackedModelInterface {
    public function getBackingStore(): ?BackingStore;
}
