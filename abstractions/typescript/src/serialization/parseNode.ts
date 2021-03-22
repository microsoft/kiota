import { Parsable } from "./parsable";

export interface ParseNode {
    getStringValue(): string;
    getChildNode(identifier: string): ParseNode;
    getBooleanValue(): boolean;
    getNumberValue(): number;
    getGuidValue(): string;
    getDateTimeOffsetValue(): Date;
    getCollectionOfPrimitiveValues<T>(): T[] | undefined;
    getCollectionOfObjectValues<T extends Parsable<T>>(): T[] | undefined;
    getObjectValue<T extends Parsable<T>>(): T;
}