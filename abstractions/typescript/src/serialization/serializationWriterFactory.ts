import { SerializationWriter } from './serializationWriter';

export interface SerializationWriterFactory {
    getSerializationWriter(contentType: string): SerializationWriter;
}