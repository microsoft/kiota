<?php


namespace Microsoft\Kiota\Abstractions\Store;

use Closure;

interface BackingStoreInterface {
    /**
     * @param string $key
     * @return mixed
     */
    public function get(string $key);

    /**
     * @param string $key
     * @param mixed|null $value
     */
    public function set(string $key, $value): void;

    /**
     * @return array<string,mixed>
     */
    public function enumerate(): array;

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
