import { Parsable } from "./parsable";
import { SerializationWriter } from "./serializationWriter";
import { SerializationWriterFactory } from "./serializationWriterFactory";

export abstract class SerializationWriterProxyFactory implements SerializationWriterFactory {
    public getValidContentType(): string {
        return this._concrete.getValidContentType();
    }
    constructor(private readonly _concrete: SerializationWriterFactory,
        private readonly _onBefore: (value: Parsable) => void,
        private readonly _onAfter: (value: Parsable) => void) {
        if(!_concrete)
            throw new Error("_concrete cannot be undefined");
    }
    public getSerializationWriter(contentType: string): SerializationWriter {
        const writer = this._concrete.getSerializationWriter(contentType);
        const originalBefore = writer.onBeforeObjectSerialization;
        const originalAfter = writer.onAfterObjectSerialization;
        writer.onBeforeObjectSerialization = (value) => {
            this._onBefore && this._onBefore(value);
            originalBefore && originalBefore(value);
        }
        writer.onAfterObjectSerialization = (value) => {
            this._onAfter && this._onAfter(value);
            originalAfter && originalAfter(value);
        }
        return writer;
    }

}