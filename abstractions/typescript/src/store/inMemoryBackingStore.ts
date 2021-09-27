import { BackingStore } from "./backingStore";
import { v4 as uuidv4 } from 'uuid';

type storeEntryWrapper = {changed: boolean, value: unknown};
type subscriptionCallback = (key: string, previousValue: unknown, newValue: unknown) => void;
type storeEntry = {key: string, value: unknown};

/** In-memory implementation of the backing store. Allows for dirty tracking of changes. */
export class InMemoryBackingStore implements BackingStore {
    public get<T>(key: string): T | undefined {
        const wrapper = this.store.get(key);
        if(wrapper && 
            (this.returnOnlyChangedValues && wrapper.changed || 
                !this.returnOnlyChangedValues)) {
            return wrapper.value as T;
        }
        return undefined;
    }
    public set<T>(key: string, value: T): void {
        var oldValueWrapper = this.store.get(key);
        var oldValue = oldValueWrapper?.value;
        if(oldValueWrapper) {
            oldValueWrapper.value = value;
            oldValueWrapper.changed = this.initializationCompleted;
        } else {
            this.store.set(key, {
                changed: this.initializationCompleted,
                value: value
            });
        }
        this.subscriptions.forEach(sub => {
            sub(key, oldValue, value);
        });
    }
    public enumerate(): storeEntry[] {
        let filterableArray = [...this.store.entries()];
        if(this.returnOnlyChangedValues) {
            filterableArray = filterableArray.filter(([_, v]) => v.changed);
        }
        return filterableArray.map(([key, value]) => {
            return {key, value};
        });
    }
    public enumerateKeysForValuesChangedToNull(): string[] {
        const keys: string[] = [];
        for(const [key, entry] of this.store) {
            if(entry.changed && !entry.value) {
                keys.push(key);
            }
        }
        return keys;
    }
    public subscribe(callback: subscriptionCallback, subscriptionId?: string | undefined): string {
        if(!callback)
            throw new Error("callback cannot be undefined");
        subscriptionId = subscriptionId ?? uuidv4();
        this.subscriptions.set(subscriptionId, callback);
        return subscriptionId;
    }
    public unsubscribe(subscriptionId: string): void {
        this.subscriptions.delete(subscriptionId);
    }
    public clear(): void {
        this.store.clear();
    }
    private readonly subscriptions: Map<string, subscriptionCallback> = new Map<string, subscriptionCallback>();
    private readonly store: Map<string, storeEntryWrapper> = new Map<string, storeEntryWrapper>();
    public returnOnlyChangedValues: boolean = false;
    private _initializationCompleted: boolean = true;
    public set initializationCompleted(value: boolean) {
        this._initializationCompleted = value;
        this.store.forEach((v) => {
            v.changed = !value;
        });
    }
    public get initializationCompleted() {
        return this._initializationCompleted;
    }
}