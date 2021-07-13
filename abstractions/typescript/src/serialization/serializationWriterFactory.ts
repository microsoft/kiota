import { SerializationWriter } from './serializationWriter';

export interface SerializationWriterFactory {
    getValidContentType(): string;
    getSerializationWriter(contentType: string): SerializationWriter;
}