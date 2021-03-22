import { Parsable, ParseNode } from "@microsoft/kiota-abstractions";

export class JsonParseNode implements ParseNode {
    /**
     *
     */
    constructor(private readonly _jsonNode: unknown) {
        
    }
    public getStringValue = (): string => this._jsonNode as string;
    public getChildNode = (identifier: string): ParseNode => new JsonParseNode((this._jsonNode as any)[identifier]);
    public getBooleanValue = (): boolean => this._jsonNode as boolean;
    public getNumberValue = (): number => this._jsonNode as number;
    public getGuidValue = (): string => this._jsonNode as string;
    public getDateValue = (): Date => this._jsonNode as Date;
    public getCollectionOfPrimitiveValues = <T>(): T[] | undefined => {
        return (this._jsonNode as unknown[])
            .map(x => new JsonParseNode(x))
            .map(x => {
                const currentParseNode = new JsonParseNode(x);
                if(x instanceof Boolean) {
                    return currentParseNode.getBooleanValue() as unknown as T;
                } else if (x instanceof String) {
                    return currentParseNode.getStringValue() as unknown as T;
                } else if (x instanceof Number) {
                    return currentParseNode.getNumberValue() as unknown as T;
                } else if (x instanceof Date) {
                    return currentParseNode.getDateValue() as unknown as T;
                } else throw new Error(`encountered an unknown type during deserialization ${typeof x}`);
            });
    }
    public getCollectionOfObjectValues = <T extends Parsable<T>>(type: new() => T): T[] | undefined => {
        return (this._jsonNode as unknown[])
            .map(x => new JsonParseNode(x))
            .map(x => x.getObjectValue<T>(type));
    }
    public getObjectValue = <T extends Parsable<T>>(type: new() => T): T => {
        const result = new type();
        this.assignFieldValues(result);
        return result;
    }
    private assignFieldValues = <T extends Parsable<T>>(item: T) : void => {
        Object.entries(this._jsonNode as any).forEach(([k, v]) => {
            const deserializer = item.deserializeFields.get(k);
            deserializer && deserializer(item, new JsonParseNode(v));
        });
    }
}