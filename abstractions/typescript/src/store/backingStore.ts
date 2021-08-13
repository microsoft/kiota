/**
* Stores model information in a different location than the object properties. Implementations can provide dirty tracking capabilities, caching capabilities or integration with 3rd party stores.
*/
export interface BackingStore {
    /**
    * Gets a value from the backing store based on its key. Returns null if the value hasn't changed and "ReturnOnlyChangedValues" is true.
    * @return The value from the backing store.
    * @param key The key to lookup the backing store with.
    */
    get<T>(key: string): T | undefined;
    /**
    * Sets or updates the stored value for the given key.
    * Will trigger subscriptions callbacks.
    * @param key The key to store and retrieve the information.
    * @param value The value to be stored.
    */
    set<T>(key: string, value: T): void;
    /**
    * Enumerates all the values stored in the backing store. Values will be filtered if "ReturnOnlyChangedValues" is true.
    * @return The values available in the backing store.
    */
    enumerate(): {key: string, value: unknown}[];
    /**
    * Enumerates the keys for all values that changed to null.
    * @return The keys for the values that changed to null.
    */
    enumerateKeysForValuesChangedToNull(): string[];
    /**
    * Creates a subscription to any data change happening.
    * @param callback Callback to be invoked on data changes where the first parameter is the data key, the second the previous value and the third the new value.
    * @param subscriptionId The subscription Id to use.
    * @return The subscription Id to use when removing the subscription
    */
    subscribe(callback:() => {key: string, previousValue: unknown, newValue: unknown}, subscriptionId?: string | undefined): string;
    /**
    * Removes a subscription from the store based on its subscription id.
    * @param subscriptionId The Id of the subscription to remove.
    */
    unsubscribe(subscriptionId: string): void;
    /**
    * Clears the data stored in the backing store. Doesn't trigger any subscription.
    */
    clear(): void;
    /**
    * Whether the initialization of the object and/or the initial deserialization has been completed to track whether objects have changed.
    */
    initializationCompleted: boolean;
    /**
    * Whether to return only values that have changed since the initialization of the object when calling the Get and Enumerate methods.
    */
    returnOnlyChangedValues: boolean;
}