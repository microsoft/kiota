import * as vscode from 'vscode';
import * as rpc from 'vscode-jsonrpc/node';
import { connectToKiota, KiotaOpenApiNode, KiotaShowConfiguration, KiotaShowResult } from './kiotaInterop';

export class OpenApiTreeProvider implements vscode.TreeDataProvider<OpenApiTreeNode> {
    private _onDidChangeTreeData: vscode.EventEmitter<OpenApiTreeNode | undefined | null | void> = new vscode.EventEmitter<OpenApiTreeNode | undefined | null | void>();
    readonly onDidChangeTreeData: vscode.Event<OpenApiTreeNode | undefined | null | void> = this._onDidChangeTreeData.event;
    constructor(private descriptionUrl: string,
        private includeFilters: string[] = [],
        private excludeFilters: string[] = []) {
        
    }
    refresh(): void {
        this._onDidChangeTreeData.fire();
    }
    getTreeItem(element: OpenApiTreeNode): vscode.TreeItem {
        return element;
    }
    private rawRootNode: KiotaOpenApiNode | undefined;
    async getChildren(element?: OpenApiTreeNode): Promise<OpenApiTreeNode[]> {
        if (element && this.rawRootNode) {
            return this.findChildren(element.path.replace('/', '').split('\\').filter(x => x !== ''), this.rawRootNode) // the root node is always /
                        .map(x => new OpenApiTreeNode(x.path, x.segment, x.children.length > 0 ? vscode.TreeItemCollapsibleState.Collapsed : vscode.TreeItemCollapsibleState.None, {
                            command: 'openapi.open',
                            title: '',
                            arguments: [this.descriptionUrl, x.path]
                        }));
        } else {
            const result = await connectToKiota(async (connection) => {
                const request = new rpc.RequestType<KiotaShowConfiguration, KiotaShowResult, void>('Show');
                return await connection.sendRequest(request, {
                    includeFilters: this.includeFilters,
                    excludeFilters: this.excludeFilters,
                    descriptionPath: this.descriptionUrl
                });
            });
            if(result && result.rootNode) {
                this.rawRootNode = result.rootNode;
                return [new OpenApiTreeNode(this.rawRootNode.path, this.rawRootNode.segment, vscode.TreeItemCollapsibleState.Collapsed, {
                    command: 'openapi.open',
                    title: '',
                    arguments: [this.descriptionUrl, this.rawRootNode.path]
                })];
            }
            return [];
        }
    }
    private findChildren(segments: string[], currentNode: KiotaOpenApiNode): KiotaOpenApiNode[] {
        if(segments.length === 0) {
            return currentNode.children;
        } else {
            const segment = segments.shift();
            if(segment) {
                const child = currentNode.children.find(x => x.segment === segment);
                if(child) {
                    return this.findChildren(segments, child);
                }
            }
        }
        return [];
    }
}

export class OpenApiTreeNode extends vscode.TreeItem {
    constructor(
        public readonly path: string,
        public readonly label: string,
        public readonly collapsibleState: vscode.TreeItemCollapsibleState,
        public readonly command?: vscode.Command
    ) {
        super(label, collapsibleState);
    }
}