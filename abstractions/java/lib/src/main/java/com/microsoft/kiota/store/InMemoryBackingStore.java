package com.microsoft.kiota.store;

import java.lang.ClassCastException;

import java.util.Map;
import java.util.HashMap;
import java.util.Objects;
import java.util.UUID;

import com.microsoft.kiota.TriConsumer;

import org.javatuples.Pair;

public class InMemoryBackingStore implements BackingStore {
    private boolean isInitializationCompleted = true;
    private boolean returnOnlyChangedValues;
    private final Map<String, Pair<Boolean, Object>> store = new HashMap<>();
    private final Map<String, TriConsumer<String, Object, Object>> subscriptionStore = new HashMap<>();
    public void setIsInitializationCompleted(final boolean value) {
        this.isInitializationCompleted = value;
        for(final Map.Entry<String, Pair<Boolean, Object>> entry : this.store.entrySet()) {
            final var wrapper = entry.getValue();
            final var updatedValue = wrapper.setAt0(Boolean.valueOf(!value));
            entry.setValue(updatedValue);
        }
    }
    public boolean getIsInitializationCompleted() {
        return this.isInitializationCompleted;
    }
    public void setReturnOnlyChangedValues(final boolean value) {
        this.returnOnlyChangedValues = value;
    }
    public boolean getReturnOnlyChangedValues() {
        return this.returnOnlyChangedValues;
    }
    public void clear() {
        this.store.clear();
    }
    public Map<String, Object> enumerate() {
        final Map<String, Object> result = new HashMap<>();
        for(final Map.Entry<String, Pair<Boolean, Object>> entry : this.store.entrySet()) {
            final Pair<Boolean, Object> wrapper = entry.getValue();
            final Object value = this.getValueFromWrapper(wrapper);

            if(value != null) {
                result.put(entry.getKey(), wrapper.getValue1());
            }
        }
        return result;
    }
    private Object getValueFromWrapper(final Pair<Boolean, Object> wrapper) {
        if(wrapper != null) {
            final Boolean hasChanged = wrapper.getValue0();
            if(!this.returnOnlyChangedValues ||
                (this.returnOnlyChangedValues && hasChanged != null && hasChanged.booleanValue())) {
                return wrapper.getValue1();
            }
        }
        return null;
    }
    @SuppressWarnings("unchecked")
    public <T> T get(final String key) {
        Objects.requireNonNull(key);
        final Pair<Boolean, Object> wrapper = this.store.get(key);
        final Object value = this.getValueFromWrapper(wrapper);
        try {
            return (T)value;
        } catch(ClassCastException ex) {
            return null;
        }
    }
    public <T> void set(final String key, final T value) {
        Objects.requireNonNull(key);
        final Pair<Boolean, Object> valueToAdd = Pair.with(Boolean.valueOf(this.isInitializationCompleted), value);
        final Pair<Boolean, Object> oldValue = this.store.put(key, valueToAdd);
        for(final TriConsumer<String, Object, Object> callback : this.subscriptionStore.values()) {
            callback.accept(key, oldValue.getValue1(), value);
        }
    }
    public void unsubscribe(final String subscriptionId) {
        Objects.requireNonNull(subscriptionId);
        this.subscriptionStore.remove(subscriptionId);
    }
    public String subscribe(final TriConsumer<String, Object, Object> callback) {
        final String subscriptionId = UUID.randomUUID().toString();
        subscribe(callback, subscriptionId);
        return subscriptionId;
    }
    public void subscribe(final TriConsumer<String, Object, Object> callback, final String subscriptionId) {
        Objects.requireNonNull(callback);
        Objects.requireNonNull(subscriptionId);
        this.subscriptionStore.put(subscriptionId, callback);
    }
}