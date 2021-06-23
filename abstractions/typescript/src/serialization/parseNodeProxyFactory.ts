import { Parsable } from "./parsable";
import { ParseNode } from "./parseNode";
import { ParseNodeFactory } from "./parseNodeFactory";

export abstract class ParseNodeProxyFactory implements ParseNodeFactory {
    public getValidContentType(): string {
        return this._concrete.getValidContentType();
    }
    constructor(private readonly _concrete: ParseNodeFactory,
        private readonly _onBefore: (value: Parsable) => void,
        private readonly _onAfter: (value: Parsable) => void) {
            if(!_concrete)
                throw new Error("_concrete cannot be undefined");
        }
    public getRootParseNode(contentType: string, content: ArrayBuffer): ParseNode {
        const node = this._concrete.getRootParseNode(contentType, content);
        var originalBefore = node.onBeforeAssignFieldValues;
        var originalAfter = node.onAfterAssignFieldValues;
        node.onBeforeAssignFieldValues = (value) => {
            this._onBefore && this._onBefore(value);
            originalBefore && originalBefore(value);
        };
        node.onAfterAssignFieldValues = (value) => {
            this._onAfter && this._onAfter(value);
            originalAfter && originalAfter(value);
        };
        return node;
    }

}