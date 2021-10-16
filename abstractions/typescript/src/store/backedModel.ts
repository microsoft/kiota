import { BackingStore } from "./backingStore";

/** Defines the contracts for a model that is backed by a store. */
export interface BackedModel {
	/**
	 * Gets the store that is backing the model.
	 */
	backingStore: BackingStore;
}
