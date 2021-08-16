package com.microsoft.kiota.store;

/** This class is used to register the backing store factory. */
public class BackingStoreFactorySingleton {
    /** The backing store factory singleton instance. */
    public static BackingStoreFactory instance = new InMemoryBackingStoreFactory();
}