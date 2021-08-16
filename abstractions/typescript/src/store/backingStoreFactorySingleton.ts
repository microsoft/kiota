import { BackingStoreFactory } from "./backingStoreFactory";
import { InMemoryBackingStoreFactory } from "./inMemoryBackingStoreFactory";

export class BackingStoreFactorySingleton {
    public static instance: BackingStoreFactory = new InMemoryBackingStoreFactory();
}