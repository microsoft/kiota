package com.microsoft.kiota.store;

/** This class is used to create instances of InMemoryBackingStore */
public class InMemoryBackingStoreFactory implements BackingStoreFactory {
    @Override
    public BackingStore createBackingStore() {
        return new InMemoryBackingStore();
    }
}
