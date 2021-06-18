package com.microsoft.kiota.store;

import javax.annotation.Nonnull;

public interface BackedModel {
    @Nonnull
    BackingStore getBackingStore();
}