import {
  SerializationWriter,
  SerializationWriterFactory,
} from "@microsoft/kiota-abstractions";

import { TextSerializationWriter } from "./textSerializationWriter";

export class TextSerializationWriterFactory
  implements SerializationWriterFactory {
  public getValidContentType(): string {
    return "text/plain";
  }
  public getSerializationWriter(contentType: string): SerializationWriter {
    if (!contentType) {
      throw new Error("content type cannot be undefined or empty");
    } else if (this.getValidContentType() !== contentType) {
      throw new Error(`expected a ${this.getValidContentType()} content type`);
    }
    return new TextSerializationWriter();
  }
}
