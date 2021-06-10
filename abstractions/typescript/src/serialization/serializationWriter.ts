import { Parsable } from "./parsable";
import { ReadableStream } from 'web-streams-polyfill/es2018';

export interface SerializationWriter {
    writeStringValue(key?: string | undefined, value?: string | undefined): void;
    writeBooleanValue(key?: string | undefined, value?: boolean | undefined): void;
    writeNumberValue(key?: string | undefined, value?: number | undefined): void;
    writeGuidValue(key?: string | undefined, value?: string | undefined): void;
    writeDateValue(key?: string | undefined, value?: Date | undefined): void;
    writeCollectionOfPrimitiveValues<T>(key?: string | undefined, values?: T[] | undefined): void;
    writeCollectionOfObjectValues<T extends Parsable>(key?: string | undefined, values?: T[]): void;
    writeObjectValue<T extends Parsable>(key?: string | undefined, value?: T | undefined): void;
    writeEnumValue<T>(key?: string | undefined, ...values: (T | undefined)[]): void;
    getSerializedContent(): ReadableStream;
    writeAdditionalData(value: Map<string, unknown>): void;
    onBeforeObjectSerialization: (value: Parsable) => void;
    onAfterObjectSerialization: (value: Parsable) => void;
}