import { BackingStore } from "./backingStore";

/** Defines the contract for a factory that creates backing stores. */
export interface BackingStoreFactory {
	/**
	 * Creates a new instance of the backing store.
	 * @return a new instance of the backing store.
	 */
	createBackingStore(): BackingStore;
}
