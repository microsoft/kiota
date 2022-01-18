<?php

namespace Microsoft\Kiota\Abstractions\Store;

use Ramsey\Uuid\Uuid;

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
        $wrapper =  $this->store[$key] ?? null;
        return $this->getValueFromWrapper($wrapper);
    }

    /**
     * @param string $key
     * @param mixed $value
     */
    public function set(string $key, $value): void
    {
        $valueToAdd = [$this->isInitializationCompleted, $value];
        $this->store[$key] = $valueToAdd;
        $oldValue = $this->store[$key];

        foreach ($this->subscriptionStore as $callback) {
            $callback($key, $oldValue[1], $value);
        }
    }

    /**
     * @return array<string,mixed>
     */
    public function enumerate(): array {
        $result = [];

        foreach ($this->store as $key => $value) {
            $val = $this->getValueFromWrapper($value);

            if ($val === null) {
                $result[$key] = $value[1];
            }
        }
        return $result;
    }

    /**
     * @param callable $callback
     * @param string|null $subscriptionId
     * @return string
     */
    public function subscribe(callable $callback, ?string $subscriptionId = null): string {
        if ($subscriptionId === null) {
            $subscriptionId = Uuid::uuid4()->toString();
        }
        $this->subscriptionStore[$subscriptionId] = $callback;
        return $subscriptionId;
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
            if ($val[1] === null && $val[0]) {
                $result []= $key;
            }
        }
        return $result;
    }

    /**
     * @param array<mixed>|null $wrapper
     * @return mixed|null
     */
    public function getValueFromWrapper(?array $wrapper) {
        if ($wrapper === null) {
            return null;
        }
        $hasChangedValue = $wrapper[0];
        if (!$this->returnOnlyChangedValues || $hasChangedValue) {
            return $wrapper[1];
        }
        return null;
    }
}