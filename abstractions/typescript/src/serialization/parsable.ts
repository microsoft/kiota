import { ParseNode } from './parseNode';
import { SerializationWriter } from './serializationWriter';

export interface Parsable<T> {
    deserializeFields(): Map<string, (item: T, node: ParseNode) => void>;
    serialize(writer: SerializationWriter): void;
}