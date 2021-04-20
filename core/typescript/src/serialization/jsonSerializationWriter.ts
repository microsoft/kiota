import { Parsable, SerializationWriter } from "@microsoft/kiota-abstractions";
import { TextEncoder } from "util";
import { ReadableStream } from 'web-streams-polyfill/es2018';

export class JsonSerializationWriter implements SerializationWriter {
    private readonly writer: string[] = [];
    private static propertySeparator = `,`;
    public writeStringValue = (key?: string, value?: string): void => {
        key && value && this.writePropertyName(key);
        value && this.writer.push(`"${value}"`);
        key && value && this.writer.push(JsonSerializationWriter.propertySeparator);
    }
    private writePropertyName = (key: string) : void => {
        this.writer.push(`"${key}":`);
    }
    public writeBooleanValue = (key?: string, value?: boolean): void => {
        key && value && this.writePropertyName(key);
        value && this.writer.push(`${value}`);
        key && value && this.writer.push(JsonSerializationWriter.propertySeparator);
    }
    public writeNumberValue = (key?: string, value?: number): void => {
        key && value && this.writePropertyName(key);
        value && this.writer.push(`${value}`);
        key && value && this.writer.push(JsonSerializationWriter.propertySeparator);
    }
    public writeGuidValue = (key?: string, value?: string): void => {
        key && value && this.writePropertyName(key);
        value && this.writer.push(`"${value}"`);
        key && value && this.writer.push(JsonSerializationWriter.propertySeparator);
    }
    public writeDateValue = (key?: string, value?: Date): void => {
        key && value && this.writePropertyName(key);
        value && this.writer.push(`"${value.toISOString()}"`);
        key && value && this.writer.push(JsonSerializationWriter.propertySeparator);
    }
    public writeCollectionOfPrimitiveValues = <T>(key?: string, values?: T[]): void => {
        if(values) {
            key && this.writePropertyName(key);
            this.writer.push(`[`);
            values.forEach(v => {
                if(v instanceof Boolean) {
                    this.writeBooleanValue(undefined, v as any as boolean);
                } else if (v instanceof String) {
                    this.writeStringValue(undefined, v as any as string);
                } else if (v instanceof Date) {
                    this.writeDateValue(undefined, v as any as Date);
                } else if (v instanceof Number) {
                    this.writeNumberValue(undefined, v as any as number);
                } else {
                    throw new Error(`encountered unknown value type during serialization ${typeof v}`);
                }
            });
            this.writer.push(`]`);
        }
    }
    public writeCollectionOfObjectValues = <T extends Parsable<T>>(key?: string, values?: T[]): void => {
        if(values) {
            key && this.writePropertyName(key);
            this.writer.push(`[`);
            values.forEach(v => {
                this.writeObjectValue(undefined, v);
                this.writer.push(JsonSerializationWriter.propertySeparator);
            });
            if(values.length > 0) { //removing the last separator
                this.writer.pop();
            }
            this.writer.push(`]`);
        }
        key && values && this.writer.push(JsonSerializationWriter.propertySeparator);
    }
    public writeObjectValue = <T extends Parsable<T>>(key?: string, value?: T): void => {
        if(value) {
            if(key) {
                this.writePropertyName(key);
            }
            this.writer.push(`{`);
            value.serialize(this);
            if(this.writer.length > 0 && this.writer[this.writer.length - 1] === JsonSerializationWriter.propertySeparator) { //removing the last separator
                this.writer.pop();
            }
            this.writer.push(`}`);
        }
    }
    public writeEnumValue = <T>(key?: string | undefined, ...values: (T | undefined)[]): void => {
        if(values.length > 0) {
            const rawValues = values.filter(x => x !== undefined).map(x => `${x}`);
            if(rawValues.length > 0) {
                this.writeStringValue(key, rawValues.reduce((x, y) => `${x}, ${y}`));
            }
        }
    }
    public getSerializedContent = (): ReadableStream<any> => {
        const encoded = new TextEncoder().encode(this.writer.join(""));
        return new ReadableStream<Uint8Array>({
            start: (controller) => {
                controller.enqueue(encoded);
                controller.close();
            }
        });
    }
}