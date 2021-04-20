import { ParseNode } from './parseNode';

export interface ParseNodeFactory {
    getRootParseNode(contentType: string, content: ArrayBuffer): ParseNode;
}