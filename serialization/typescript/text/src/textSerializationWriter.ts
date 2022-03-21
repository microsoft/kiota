import {
  DateOnly,
  Duration,
  Parsable,
  SerializationWriter,
  TimeOnly,
} from "@microsoft/kiota-abstractions";

export class TextSerializationWriter implements SerializationWriter {
  private static noStructuredDataMessage =
    "text does not support structured data";
  private readonly writer: string[] = [];
  public onBeforeObjectSerialization: ((value: Parsable) => void) | undefined;
  public onAfterObjectSerialization: ((value: Parsable) => void) | undefined;
  public onStartObjectSerialization:
    | ((value: Parsable, writer: SerializationWriter) => void)
    | undefined;
  public writeStringValue = (key?: string, value?: string): void => {
    if (key || key !== "") {
      throw new Error(TextSerializationWriter.noStructuredDataMessage);
    }
    if (value) {
      if (this.writer.length > 0) {
        throw new Error(
          "a value was already written for this serialization writer, text content only supports a single value"
        );
      } else {
        this.writer.push(value);
      }
    }
  };
  public writeBooleanValue = (key?: string, value?: boolean): void => {
    if (value) {
      this.writeStringValue(key, `${value}`);
    }
  };
  public writeNumberValue = (key?: string, value?: number): void => {
    if (value) {
      this.writeStringValue(key, `${value}`);
    }
  };
  public writeGuidValue = (key?: string, value?: string): void => {
    if (value) {
      this.writeStringValue(key, `"${value}"`);
    }
  };
  public writeDateValue = (key?: string, value?: Date): void => {
    if (value) {
      this.writeStringValue(key, `"${value.toISOString()}"`);
    }
  };
  public writeDateOnlyValue = (key?: string, value?: DateOnly): void => {
    if (value) {
      this.writeStringValue(key, `"${value.toString()}"`);
    }
  };
  public writeTimeOnlyValue = (key?: string, value?: TimeOnly): void => {
    if (value) {
      this.writeStringValue(key, `"${value.toString()}"`);
    }
  };
  public writeDurationValue = (key?: string, value?: Duration): void => {
    if (value) {
      this.writeStringValue(key, `"${value.toString()}"`);
    }
  };
  public writeNullValue = (key?: string): void => {
    this.writeStringValue(key, `null`);
  };
  public writeCollectionOfPrimitiveValues = <T>(
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    key?: string,
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    values?: T[]
  ): void => {
    throw new Error(TextSerializationWriter.noStructuredDataMessage);
  };
  public writeCollectionOfObjectValues = <T extends Parsable>(
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    key?: string,
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    values?: T[]
  ): void => {
    throw new Error(TextSerializationWriter.noStructuredDataMessage);
  };
  public writeObjectValue = <T extends Parsable>(
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    key?: string,
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    value?: T
  ): void => {
    throw new Error(TextSerializationWriter.noStructuredDataMessage);
  };
  public writeEnumValue = <T>(
    key?: string | undefined,
    ...values: (T | undefined)[]
  ): void => {
    if (values.length > 0) {
      const rawValues = values
        .filter((x) => x !== undefined)
        .map((x) => `${x}`);
      if (rawValues.length > 0) {
        this.writeStringValue(
          key,
          rawValues.reduce((x, y) => `${x}, ${y}`)
        );
      }
    }
  };
  public getSerializedContent = (): ArrayBuffer => {
    return this.convertStringToArrayBuffer(this.writer.join(``));
  };

  private convertStringToArrayBuffer = (str: string): ArrayBuffer => {
    const arrayBuffer = new ArrayBuffer(str.length);
    const uint8Array = new Uint8Array(arrayBuffer);
    for (let i = 0; i < str.length; i++) {
      uint8Array[i] = str.charCodeAt(i);
    }
    return arrayBuffer;
  };

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  public writeAdditionalData = (value: Record<string, unknown>): void => {
    throw new Error(TextSerializationWriter.noStructuredDataMessage);
  };
}
