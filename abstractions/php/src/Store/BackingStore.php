<?php


namespace Microsoft\Kiota\Abstractions\Store;

use Closure;

interface BackingStore {
    /**
     * Gets a value from the backing store based on its key. Returns null if the value hasn't changed and "ReturnOnlyChangedValues" is true.
     * @param string $key
     * @return mixed The value from the backing store.
     */
    public function get(string $key);

    /**
     * Sets or updates the stored value for the given key.
     * Will trigger subscription callbacks
     *
     * @param string $key The key to store and retrieve information.
     * @param mixed|null $value The value to be $associated with the given key.
     */
    public function set(string $key, $value): void;

    /**
     * Enumerates all the values stored in the backing store. Values will be filtered if "ReturnOnlyChangedValues" is true.
     * @return array<string,mixed> the array of key-value pairs available in
     *  the backing store
     */
    public function enumerate(): array;

    /**
     * Enumerates the keys for all values that changed to null
     * @return iterable<string>
     */
    public function enumerateKeysForValuesChangedToNull(): iterable;

    /**
     * Creates a subscription to any data change happening.
     * @param callable $callback Callback to be invoked on data changes where the first parameter is the data key, the second the previous value and the third the new value.
     * @param string|null $subscriptionId
     * @return string The subscription ID to use when removing the subscription
     */
    public function subscribe(callable $callback, ?string $subscriptionId = null): string;

    /**
     * Removes a subscription from the store based on its subscription id.
     * @param string $subscriptionId The Id of the subscription to remove.
     */
    public function unsubscribe(string $subscriptionId): void;

    /**
     * Clears the data stored in the backing store. Doesn't trigger any subscription.
     */
    public function clear(): void;

    /**
     * Sets whether the initialization of the object and/or the initial deserialization has been completed to track whether objects have changed.
     * @param bool $value value to set
     */
    public function setIsInitializationCompleted(bool $value): void;

    /**
     * @return bool Whether the initialization of the object and/or the initial deserialization has been completed to track whether objects have changed.
     */
    public function getIsInitializationCompleted(): bool;

    /**
     * Sets whether to return only values that have changed since the initialization of the object when calling the Get and Enumerate methods.
     * @param bool $value value to set
     */
    public function setReturnOnlyChangedValues(bool $value): void;

    /**
     * @return bool Whether to return only values that have changed since the initialization of the object when calling the Get and Enumerate methods.
     */
    public function getReturnOnlyChangedValues(): bool;

}
