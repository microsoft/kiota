import { ParseNode } from "./parseNode";
import { SerializationWriter } from "./serializationWriter";

/**
 * Defines a serializable model object.
 */
export interface Parsable {
  /**
   * Gets the deserialization information for this object.
   * @return The deserialization information for this object where each entry is a property key with its deserialization callback.
   */
  getFieldDeserializers<T>(): Record<string, (item: T, node: ParseNode) => void>;
  /**
   * Writes the objects properties to the current writer.
   * @param writer The writer to write to.
   */
  serialize(writer: SerializationWriter): void;
  /**
   * Gets the additional data for this object that did not belong to the properties.
   * @return The additional data for this object.
   */
  additionalData: Record<string, unknown>;
}
