import { ParseNode } from "./parseNode";
import { ParseNodeFactory } from "./parseNodeFactory";

export class ParseNodeFactoryRegistry implements ParseNodeFactory {
    public static readonly defaultInstance = new ParseNodeFactoryRegistry();
    public getValidContentType(): string {
        throw new Error("The registry supports multiple content types. Get the registered factory instead.");
    }
    public contentTypeAssociatedFactories = new Map<string, ParseNodeFactory>();
    public getRootParseNode(contentType: string, content: ArrayBuffer): ParseNode {
        if(!contentType) {
            throw new Error("content type cannot be undefined or empty");
        }
        if(!content) {
            throw new Error("content cannot be undefined or empty");
        }
        const factory = this.contentTypeAssociatedFactories.get(contentType);
        if(factory) {
            return factory.getRootParseNode(contentType, content);
        } else {
            throw new Error(`Content type ${contentType} does not have a factory registered to be parsed`);
        }
    }

}