package com.microsoft.kiota.serialization;

import java.util.Map;

import javax.annotation.Nonnull;

/** Defines a contract for models that can hold additional data besides the described properties. */
public interface AdditionalDataHolder {
    /**
     * Gets the additional data for this object that did not belong to the properties.
     * @return The additional data for this object.
     */
    @Nonnull
    Map<String, Object> getAdditionalData();    
}
