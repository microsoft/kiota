import { ParseNode } from './parseNode';

/**
 * Defines the contract for a factory that is used to create {@link ParseNode}s.
 */
export interface ParseNodeFactory {
    /**
     * Returns the content type this factory's parse nodes can deserialize.
     */
    getValidContentType(): string;
    /**
     * Creates a {@link ParseNode} from the given {@link ArrayBuffer} and content type.
     * @param content the {@link ArrayBuffer} to read from.
     * @param contentType the content type of the {@link ArrayBuffer}.
     * @return a {@link ParseNode} that can deserialize the given {@link ArrayBuffer}.
     */
    getRootParseNode(contentType: string, content: ArrayBuffer): ParseNode;
}