import { Parsable } from "./parsable";

/** Defines an interface for serialization of objects to a stream. */
export interface SerializationWriter {
    /**
     * Writes the specified string value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    writeStringValue(key?: string | undefined, value?: string | undefined): void;
    /**
     * Writes the specified boolean value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    writeBooleanValue(key?: string | undefined, value?: boolean | undefined): void;
    /**
     * Writes the specified number value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    writeNumberValue(key?: string | undefined, value?: number | undefined): void;
    /**
     * Writes the specified Guid value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    writeGuidValue(key?: string | undefined, value?: string | undefined): void;
    /**
     * Writes the specified Date value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    writeDateValue(key?: string | undefined, value?: Date | undefined): void;
    /**
     * Writes the specified collection of primitive values to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    writeCollectionOfPrimitiveValues<T>(key?: string | undefined, values?: T[] | undefined): void;
    /**
     * Writes the specified collection of object values to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    writeCollectionOfObjectValues<T extends Parsable>(key?: string | undefined, values?: T[]): void;
    /**
     * Writes the specified model object value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    writeObjectValue<T extends Parsable>(key?: string | undefined, value?: T | undefined): void;
    /**
     * Writes the specified enum value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param values the value to write to the stream.
     */
    writeEnumValue<T>(key?: string | undefined, ...values: (T | undefined)[]): void;
    /**
     * Writes a null value for the specified key.
     * @param key the key to write the value with.
     */
    writeNullValue(key?: string | undefined) : void;
    /**
     * Gets the value of the serialized content.
     * @return the value of the serialized content.
     */
    getSerializedContent(): ArrayBuffer;
    /**
     * Writes the specified additional data values to the stream with an optional given key.
     * @param value the values to write to the stream.
     */
    writeAdditionalData(value: Map<string, unknown>): void;
    /**
     * Gets the callback called before the object gets serialized.
     * @return the callback called before the object gets serialized.
     */
    onBeforeObjectSerialization: ((value: Parsable) => void) | undefined;
    /**
     * Gets the callback called after the object gets serialized.
     * @return the callback called after the object gets serialized.
     */
    onAfterObjectSerialization: ((value: Parsable) => void) | undefined;
    /**
     * Gets the callback called right after the serialization process starts.
     * @return the callback called right after the serialization process starts.
     */
     onStartObjectSerialization: ((value: Parsable, writer: SerializationWriter) => void) | undefined;
}