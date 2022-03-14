<?php

namespace Microsoft\Kiota\Abstractions\Serialization;

interface AdditionalDataHolder {
    /**
     * Gets the additional data for this object that did not belong to the properties.
     * @return array<string,mixed> The additional data for this object.
     */
    public function getAdditionalData(): array;

    /**
     * Sets the additional data for this object that did not belong to the properties.
     * @param array<string, mixed> $value The additional data for this object.
     */
    public function setAdditionalData(array $value): void;
}