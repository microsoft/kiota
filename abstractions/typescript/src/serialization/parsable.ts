import { ParseNode } from './parseNode';
import { SerializationWriter } from './serializationWriter';

export interface Parsable {
    getFieldDeserializers<T>(): Map<string, (item: T, node: ParseNode) => void>;
    serialize(writer: SerializationWriter): void;
    additionalData: Map<string, unknown>;
}