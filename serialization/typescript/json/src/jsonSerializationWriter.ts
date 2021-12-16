import { Parsable, SerializationWriter } from "@microsoft/kiota-abstractions";

export class JsonSerializationWriter implements SerializationWriter {
    private readonly writer: string[] = [];
    private static propertySeparator = `,`;
    public onBeforeObjectSerialization: ((value: Parsable) => void) | undefined;
    public onAfterObjectSerialization: ((value: Parsable) => void) | undefined;
    public onStartObjectSerialization: ((value: Parsable, writer: SerializationWriter) => void) | undefined;
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
    public writeNullValue = (key?: string): void => {
        key && this.writePropertyName(key);
        this.writer.push(`null`);
        key && this.writer.push(JsonSerializationWriter.propertySeparator);
    }
    public writeCollectionOfPrimitiveValues = <T>(key?: string, values?: T[]): void => {
        if(values) {
            key && this.writePropertyName(key);
            this.writer.push(`[`);
            values.forEach((v, idx) => {
                this.writeAnyValue(undefined, v);
                (idx + 1) < values.length && this.writer.push(JsonSerializationWriter.propertySeparator);
            });
            this.writer.push(`]`);
            key && this.writer.push(JsonSerializationWriter.propertySeparator);
        }
    }
    public writeCollectionOfObjectValues = <T extends Parsable>(key?: string, values?: T[]): void => {
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
            key && this.writer.push(JsonSerializationWriter.propertySeparator);
        }
    }
    public writeObjectValue = <T extends Parsable>(key?: string, value?: T): void => {
        if(value) {
            if(key) {
                this.writePropertyName(key);
            }
            this.onBeforeObjectSerialization && this.onBeforeObjectSerialization(value);
            this.writer.push(`{`);
            this.onStartObjectSerialization && this.onStartObjectSerialization(value, this);
            value.serialize(this);
            this.onAfterObjectSerialization && this.onAfterObjectSerialization(value);
            if(this.writer.length > 0 && this.writer[this.writer.length - 1] === JsonSerializationWriter.propertySeparator) { //removing the last separator
                this.writer.pop();
            }
            this.writer.push(`}`);
            key && this.writer.push(JsonSerializationWriter.propertySeparator);
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
    public getSerializedContent = (): ArrayBuffer=> {
        return this.convertStringToArrayBuffer(this.writer.join(``));
    }
    
    private convertStringToArrayBuffer = (str: string): ArrayBuffer => {
        const arrayBuffer = new ArrayBuffer(str.length);
        const uint8Array = new Uint8Array(arrayBuffer);
        for (let i = 0; i < str.length; i++) {
          uint8Array[i] = str.charCodeAt(i);
        }
        return arrayBuffer;
    }

    public writeAdditionalData = (value: Map<string, unknown>) : void => {
        if(!value) return;

        value.forEach((dataValue, key) => {
            this.writeAnyValue(key, dataValue);
        });
    }
    private writeNonParsableObjectValue = (key?: string | undefined, value?: object | undefined) => {
        if(key) {
            this.writePropertyName(key);
        }
        this.writer.push(JSON.stringify(value), JsonSerializationWriter.propertySeparator);
    }
    private writeAnyValue = (key?: string | undefined, value?: unknown | undefined) : void => {
        if(value) {
            const valueType = typeof value;
            if(!value) {
                this.writeNullValue(key);
            }else if(valueType === "boolean") {
                this.writeBooleanValue(key, value as any as boolean);
            } else if (valueType === "string") {
                this.writeStringValue(key, value as any as string);
            } else if (value instanceof Date) {
                this.writeDateValue(key, value as any as Date);
            } else if (valueType === "number") {
                this.writeNumberValue(key, value as any as number);
            } else if(Array.isArray(value)) {
                this.writeCollectionOfPrimitiveValues(key, value);
            } else if (valueType === "object") {
                this.writeNonParsableObjectValue(key, value as any as object);
            } else {
                throw new Error(`encountered unknown value type during serialization ${valueType}`);
            }
        } else {
            if(key)
                this.writePropertyName(key)
            this.writer.push("null");
        }
    }
}