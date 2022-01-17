<?php

namespace Microsoft\Kiota\Abstractions\Store;

use Closure;

class InMemoryBackingStore implements BackingStore
{

    private bool $isInitializationCompleted = true;
    private bool $returnOnlyChangedValues;

    /**
     * @var array<string,array|mixed|array<string,mixed>> $store;
     */
    private array $store = [];

    /** @var array<string, callable> $subscriptionStore */
    private array $subscriptionStore = [];
    /**
     * @param string $key
     * @return mixed
     */
    public function get(string $key) {

        return $this->store[$key] ?? null;
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
     * @param callable $callback
     * @param string|null $subscriptionId
     * @return string|null
     */
    public function subscribe(callable $callback, ?string $subscriptionId = null): ?string {
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

    /**
     * @return iterable<string>
     */
    public function enumerateKeysForValuesChangedToNull(): iterable {
        $result = [];

        foreach ($this->store as $key => $val) {
            $wrapper = $val;
            $value = $wrapper[1];
            if ($value === null && $wrapper[0]) {
                $result []= $key;
            }
        }
        return $result;
    }
}