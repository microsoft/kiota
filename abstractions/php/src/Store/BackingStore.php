<?php


namespace Microsoft\Kiota\Abstractions\Store;

use Closure;

interface BackingStore {
    /**
     * @param string $key
     * @return mixed
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
     *
     * @return array<string,mixed> the array of key-value pairs available in
     *  the backing store
     */
    public function enumerate(): array;

    /**
     * Enumerates the keys for all values that changed to null
     * @return iterable
     */
    public function enumerateKeysForValuesChangedToNull(): iterable;

    /**
     * @param Closure $callback
     * @param string|null $subscriptionId
     * @return string
     */
    public function subscribe(Closure $callback, ?string $subscriptionId = null): ?string;

    /**
     * @param string $subscriptionId
     */
    public function unsubscribe(string $subscriptionId): void;

    /**
     * @return void
     */
    public function clear(): void;

    /**
     * @param bool $value
     * @return void
     */
    public function setIsInitializationCompleted(bool $value): void;

    /**
     * @return bool
     */
    public function getIsInitializationCompleted(): bool;

    /**
     * @return void
     */
    public function setReturnOnlyChangedValues(bool $value): void;

    /**
     * @return bool
     */
    public function getReturnOnlyChangedValues(): bool;

}
