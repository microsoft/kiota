<?php

namespace Microsoft\Kiota\Abstractions\Store;

use Closure;

class InMemoryBackingStore implements BackingStore
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
    public function get(string $key) {

        if (!array_key_exists($key, $this->store)) {
            return null;
        }
        return $this->store[$key];
    }

    /**
     * @param string $key
     * @param mixed $value
     */
    public function set(string $key, $value): void
    {
        $this->store[$key] = $value;
    }

    /**
     * @return array<string,mixed>
     */
    public function enumerate(): array {
        return [];
    }

    /**
     * @param Closure $callback
     * @param string|null $subscriptionId
     * @return string|null
     */
    public function subscribe(Closure $callback, ?string $subscriptionId = null): ?string {
        return '';
    }

    /**
     * @param string $subscriptionId
     */
    public function unsubscribe(string $subscriptionId): void {
        unset($this->subscriptionStore[$subscriptionId]);
    }

    /**
     *
     */
    public function clear(): void {
        $this->store = [];
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