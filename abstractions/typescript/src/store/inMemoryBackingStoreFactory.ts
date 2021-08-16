import { BackingStore } from "./backingStore";
import { BackingStoreFactory } from "./backingStoreFactory";
import { InMemoryBackingStore } from "./inMemoryBackingStore";

/** This class is used to create instances of InMemoryBackingStore */
export class InMemoryBackingStoreFactory implements BackingStoreFactory {
    public createBackingStore(): BackingStore {
        return new InMemoryBackingStore();
    }
}