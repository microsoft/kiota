<?php

namespace Microsoft\Kiota\Abstractions\Store;

use Closure;

class InMemoryBackingStore implements BackingStoreInterface
{

    private bool $isInitializationCompleted = true;
    private bool $returnOnlyChangedValues;
    private array $store = [];

    /** @var array<string, callable> $subscriptionStore */
    private array $subscriptionStore = [];
    /**
     * @param string $key
     * @return mixed
     */
    public function get(string $key)
    {
        // TODO: Implement get() method.
    }

    /**
     * @param string $key
     * @param mixed $value
     */
    public function set(string $key, $value): void
    {
        // TODO: Implement set() method.
    }

    /**
     * @return array<string,mixed>
     */
    public function enumerate(): array {
        // TODO: Implement enumerate() method.
    }

    /**
     * @param Closure $callback
     * @param string|null $subscriptionId
     * @return string|null
     */
    public function subscribe(Closure $callback, ?string $subscriptionId = null): ?string {
        // TODO: Implement subscribe() method.
    }

    /**
     * @param string $subscriptionId
     */
    public function unsubscribe(string $subscriptionId): void {
        // TODO: Implement unsubscribe() method.
    }

    /**
     *
     */
    public function clear(): void {
        
    }

    /**
     * @param bool $value
     */
    public function setIsInitializationCompleted(bool $value): void {
        $this->isInitializationCompleted = $value;
    }

    /**
     * @return bool
     */
    public function getIsInitializationCompleted(): bool {
        return $this->isInitializationCompleted;
    }

    /**
     *
     */
    public function setReturnOnlyChangedValues(bool $value): void {
        $this->returnOnlyChangedValues = $value;
    }

    /**
     * @return bool
     */
    public function getReturnOnlyChangedValues(): bool {
        return $this->returnOnlyChangedValues;
    }
}