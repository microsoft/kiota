import { SerializationWriter, SerializationWriterFactory } from "@microsoft/kiota-abstractions";
import { JsonSerializationWriter } from "./jsonSerializationWriter";

export class JsonSerializationWriterFactory implements SerializationWriterFactory {
    private static validContentType = "application/json";
    public getSerializationWriter(contentType: string): SerializationWriter {
        if(!contentType) {
            throw new Error("content type cannot be undefined or empty");
        } else if (JsonSerializationWriterFactory.validContentType !== contentType) {
            throw new Error(`expected a ${JsonSerializationWriterFactory.validContentType} content type`);
        }
        return new JsonSerializationWriter();
    }
}