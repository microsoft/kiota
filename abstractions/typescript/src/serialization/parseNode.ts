import { Parsable } from "./parsable";

/**
 * Interface for a deserialization node in a parse tree. This interace provides an abstraction layer over serialiation formats, libararies and implementations.
 */
export interface ParseNode {
    /**
     * Gets the string value of the node.
     * @return the string value of the node.
     */
    getStringValue(): string;
    /**
     * Gets a new parse node for the given identifier.
     * @param identitier the identifier of the current node property.
     * @return a new parse node for the given identifier.
     */
    getChildNode(identifier: string): ParseNode;
    /**
     * Gets the boolean value of the node.
     * @return the boolean value of the node.
     */
    getBooleanValue(): boolean;
    /**
     * Gets the Number value of the node.
     * @return the Number value of the node.
     */
    getNumberValue(): number;
    /**
     * Gets the Guid value of the node.
     * @return the Guid value of the node.
     */
    getGuidValue(): string; //TODO https://www.npmjs.com/package/guid-typescript
    /**
     * Gets the Date value of the node.
     * @return the Date value of the node.
     */
    getDateValue(): Date;
    /**
     * Gets the collection of primitive values of the node.
     * @return the collection of primitive values of the node.
     */
    getCollectionOfPrimitiveValues<T>(): T[] | undefined;
    /**
     * Gets the collection of object values of the node.
     * @return the collection of object values of the node.
     */
    getCollectionOfObjectValues<T extends Parsable>(type: new() => T): T[] | undefined;
    /**
     * Gets the model object value of the node.
     * @return the model object value of the node.
     */
    getObjectValue<T extends Parsable>(type: new() => T): T;
    /**
     * Gets the Enum values of the node.
     * @return the Enum values of the node.
     */
    getEnumValues<T>(type: any): T[];
    /**
     * Gets the Enum value of the node.
     * @return the Enum value of the node.
     */
    getEnumValue<T>(type: any): T | undefined;
    /**
     * Gets the callback called before the node is deserialized.
     * @return the callback called before the node is deserialized.
     */
    onBeforeAssignFieldValues: ((value: Parsable) => void) | undefined;
    /**
     * Gets the callback called after the node is deseserialized.
     * @return the callback called after the node is deserialized.
     */
    onAfterAssignFieldValues: ((value: Parsable) => void) | undefined;
}