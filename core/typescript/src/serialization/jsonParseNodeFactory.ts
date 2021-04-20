import { ParseNode, ParseNodeFactory } from "@microsoft/kiota-abstractions";
import { TextDecoder } from "util";
import { JsonParseNode } from "./jsonParseNode";

export class JsonParseNodeFactory implements ParseNodeFactory {
    private static validContentType = "application/json";
    public getRootParseNode(contentType: string, content: ArrayBuffer): ParseNode {
        if(!contentType) {
            throw new Error("content type cannot be undefined or empty");
        } else if (JsonParseNodeFactory.validContentType !== contentType) {
            throw new Error(`expected a ${JsonParseNodeFactory.validContentType} content type`);
        }
        if(!content) {
            throw new Error("content cannot be undefined of empty");
        }
        const decoder = new TextDecoder();
        const contentAsStr = decoder.decode(content);
        const contentAsJson = JSON.parse(contentAsStr);
        return new JsonParseNode(contentAsJson);
    }
}