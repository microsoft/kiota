import { SerializationWriter } from "./serializationWriter";
import { SerializationWriterFactory } from "./serializationWriterFactory";

/** This factory holds a list of all the registered factories for the various types of nodes. */
export class SerializationWriterFactoryRegistry implements SerializationWriterFactory {
    /** Default singleton instance of the registry to be used when registring new factories that should be available by default. */
    public static readonly defaultInstance = new SerializationWriterFactoryRegistry();
    public getValidContentType(): string {
        throw new Error("The registry supports multiple content types. Get the registered factory instead.");
    }
    /** List of factories that are registered by content type. */
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