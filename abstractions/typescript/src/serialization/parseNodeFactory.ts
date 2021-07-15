import { ParseNode } from './parseNode';

export interface ParseNodeFactory {
    getValidContentType(): string;
    getRootParseNode(contentType: string, content: ArrayBuffer): ParseNode;
}