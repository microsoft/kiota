import { ParseNode, ParseNodeFactory } from "@microsoft/kiota-abstractions";

export class ParseNodeFactoryRegistry implements ParseNodeFactory {
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