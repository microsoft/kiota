export interface BackingStore {
    get<T>(key: string): T | undefined;
    set<T>(key: string, value: T): void;
    enumerate(): {key: string, value: unknown}[];
    subscribe(callback:() => {key: string, previousValue: unknown, newValue: unknown}): string;
    unsubscribe(subscriptionId: string): void;
    clear(): void;
    initializationCompleted: boolean;
    returnOnlyChangedValues: boolean;
}