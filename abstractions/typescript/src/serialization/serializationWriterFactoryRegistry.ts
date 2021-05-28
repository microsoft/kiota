import { SerializationWriter, SerializationWriterFactory } from "@microsoft/kiota-abstractions";

export class SerializationWriterFactoryRegistry implements SerializationWriterFactory {
    public contentTypeAssociatedFactories = new Map<string, SerializationWriterFactory>();
    public getSerializationWriter(contentType: string): SerializationWriter {
        if(!contentType) {
            throw new Error("content type cannot be undefined or empty");
        }
        const factory = this.contentTypeAssociatedFactories.get(contentType);
        if(factory) {
            return factory.getSerializationWriter(contentType);
        } else {
            throw new Error(`Content type ${contentType} does not have a factory registered to be serialized`);
        }
    }

}