import * as path from 'path';
import * as vscode from 'vscode';
import * as rpc from 'vscode-jsonrpc/node';
import { connectToKiota, KiotaOpenApiNode, KiotaShowConfiguration, KiotaShowResult, LockFile } from './kiotaInterop';

export class OpenApiTreeProvider implements vscode.TreeDataProvider<OpenApiTreeNode> {
    private _onDidChangeTreeData: vscode.EventEmitter<OpenApiTreeNode | undefined | null | void> = new vscode.EventEmitter<OpenApiTreeNode | undefined | null | void>();
    readonly onDidChangeTreeData: vscode.Event<OpenApiTreeNode | undefined | null | void> = this._onDidChangeTreeData.event;
    constructor(
        private readonly context: vscode.ExtensionContext,
        private _descriptionUrl?: string,
        public readonly includeFilters: string[] = [],
        public readonly excludeFilters: string[] = []) {
        
    }
    private _lockFilePath?: string;
    private _lockFile?: LockFile;
    public get isLockFileLoaded(): boolean {
        return !!this._lockFile;
    }
    public async loadLockFile(path: string): Promise<void> {
      this.closeDescription(false);
      this._lockFilePath = path;
      const lockFileData = await vscode.workspace.fs.readFile(vscode.Uri.file(path));
      this._lockFile = JSON.parse(lockFileData.toString()) as LockFile;
      if (this._lockFile?.descriptionLocation) {
        this._descriptionUrl = this._lockFile.descriptionLocation;
        await this.loadNodes();
        if (this.rawRootNode) {
            if (this._lockFile.includePatterns.length === 0) {
                this.setAllSelected(this.rawRootNode, true);
            } else {
                this._lockFile.includePatterns.forEach(ip => {
                    const currentNode = this.findApiNode(ip.split(pathSeparator).filter(x => x !== '').map(x => x.split(operationSeparator)).flat(1), this.rawRootNode!);
                    if(currentNode) {
                        currentNode.selected = true;
                        if (!(currentNode.isOperation || false)) {
                            currentNode.children.filter(x => x.isOperation || false).forEach(x => x.selected = true);
                        }
                    }
                });
            }
            this.refreshView();
        }
      }
    }
    private setAllSelected(node: KiotaOpenApiNode, selected: boolean) {
        node.selected = selected;
        node.children.forEach(x => this.setAllSelected(x, selected));
    }
    public get outputPath(): string {
      return this._lockFilePath ? path.parse(this._lockFilePath).dir : '';
    }
    public get clientClassName(): string {
        return this._lockFile?.clientClassName || '';
    }
    public get clientNamespaceName(): string {
        return this._lockFile?.clientNamespaceName || '';
    }
    public get language(): string {
        return this._lockFile?.language || '';
    }
    public closeDescription(shouldRefresh = true) {
        this._descriptionUrl = '';
        this.rawRootNode = undefined;
        this._lockFile = undefined;
        this._lockFilePath = undefined;
        this.tokenizedFilter = [];
        this._filterText = '';
        if (shouldRefresh) {
            this.refreshView();
        }
    }
    public async setDescriptionUrl(descriptionUrl: string): Promise<void> {
        this.closeDescription(false);
        this._descriptionUrl = descriptionUrl;
        await this.loadNodes();
        this.refreshView();
    }
    public get descriptionUrl(): string {
        return this._descriptionUrl || '';
    }
    public select(item: OpenApiTreeNode, selected: boolean, recursive: boolean): void {
        if (!this.rawRootNode) {
            return;
        }
        const apiNode = this.findApiNode(getPathSegments(item.path), this.rawRootNode);
        if(apiNode) {
            this.selectInternal(apiNode, selected, recursive);
            this.refreshView();
        }
    }
    private selectInternal(apiNode: KiotaOpenApiNode, selected: boolean, recursive: boolean) {
        apiNode.selected = selected;
        const isOperationNode = apiNode.isOperation || false;
        if(recursive) {
            apiNode.children.forEach(x => this.selectInternal(x, selected, recursive));
        } else if (!isOperationNode) {
            apiNode.children.filter(x => x.isOperation || false).forEach(x => this.selectInternal(x, selected, false));
        } else if (isOperationNode && !selected && this.rawRootNode) {
            const parent = this.findApiNode(getPathSegments(trimOperation(apiNode.path)), this.rawRootNode);
            if (parent) {
                parent.selected = selected;
            }
        }
    }
    private findApiNode(segments: string[], currentNode: KiotaOpenApiNode): KiotaOpenApiNode | undefined {
        if (segments.length === 0) {
            return currentNode;
        }
        const segment = segments.shift();
        if (segment) {
            const child = currentNode.children.find(x => x.segment === segment);
            if (child) {
                return this.findApiNode(segments, child);
            }
        }
        return undefined;
    }

    refreshView(): void {
        this._onDidChangeTreeData.fire();
    }
    getTreeItem(element: OpenApiTreeNode): vscode.TreeItem {
        return element;
    }
    public getSelectedPaths(): string[] {
        if (!this.rawRootNode) {
            return [];
        }
        return this.findSelectedPaths(this.rawRootNode).map(x => x === '' ? pathSeparator : x); // root node trailing slash is /
    }
    private findSelectedPaths(currentNode: KiotaOpenApiNode): string[] {
        const result: string[] = [];
        if(currentNode.selected || false) {
            if ((currentNode.isOperation || false) && this.rawRootNode) {
                const parent = this.findApiNode(getPathSegments(trimOperation(currentNode.path)), this.rawRootNode);
                if (parent && !parent.selected) {
                    result.push(currentNode.path.replace(/\\/g, pathSeparator));
                }
            } else {
                result.push(currentNode.path.replace(/\\/g, pathSeparator));
            }
        }
        currentNode.children.forEach(x => result.push(...this.findSelectedPaths(x)));
        return result;
    }
    private rawRootNode: KiotaOpenApiNode | undefined;
    private tokenizedFilter: string[] = [];
    private _filterText: string = '';
    public set filter(filterText: string) {
        this._filterText = filterText;
        if (!this.rawRootNode) {
            return;
        }
        this.tokenizedFilter = filterText.length === 0 ? [] : filterText.split(' ').filter(x => x !== '').map(x => x.trim().toLowerCase());
        this.refreshView();
    }
    public get filter(): string {
        return this._filterText;
    }
    private async loadNodes(): Promise<void> {
        if (!this.descriptionUrl || this.descriptionUrl.length === 0) {
            return;
        }
        const result = await connectToKiota(this.context, async (connection) => {
            const request = new rpc.RequestType<KiotaShowConfiguration, KiotaShowResult, void>('Show');
            return await connection.sendRequest(request, {
                includeFilters: this.includeFilters,
                excludeFilters: this.excludeFilters,
                descriptionPath: this.descriptionUrl
            });
        });
        if(result && result.rootNode) {
            this.rawRootNode = result.rootNode;
        }
    }
    getCollapsedState(hasChildren: boolean): vscode.TreeItemCollapsibleState {
        return !hasChildren ?
                vscode.TreeItemCollapsibleState.None :
                (this.tokenizedFilter.length === 0 ?
                    vscode.TreeItemCollapsibleState.Collapsed : 
                    vscode.TreeItemCollapsibleState.Expanded);
    }
    getTreeNodeFromKiotaNode(node: KiotaOpenApiNode): OpenApiTreeNode {
        return new OpenApiTreeNode(
            node.path, 
            node.segment,
            node.selected || false,
            this.getCollapsedState(node.children.length > 0),
            node.isOperation || false,
            node.children.map(x => this.getTreeNodeFromKiotaNode(x)),
            node.documentationUrl
        );
    }
    getChildren(element?: OpenApiTreeNode): OpenApiTreeNode[] {
        if (!this.rawRootNode) {
            return [];
        }
        if (element) {
            return element.children.filter(x => x.isNodeVisible(this.tokenizedFilter));
        } else {
            const result = this.getTreeNodeFromKiotaNode(this.rawRootNode);
            result.collapsibleState = vscode.TreeItemCollapsibleState.Expanded;
            return [result];
        }
    }
}
const operationSeparator = '#';
const pathSeparator = '/';
const operationsNames = new Set<string>(['get', 'put', 'post', 'patch', 'delete', 'head', 'options', 'trace']);
function getPathSegments(path: string): string[] {
    return path.replace(pathSeparator, '').split('\\').map(x => x.split(operationSeparator)).flat(1).filter(x => x !== ''); // the root node is always /
}
function trimOperation(path: string): string {
    return path.split(operationSeparator)[0];
}
type IconSet = string | vscode.Uri | { light: string | vscode.Uri; dark: string | vscode.Uri } | vscode.ThemeIcon;
export class OpenApiTreeNode extends vscode.TreeItem {
    private static readonly selectedSet: IconSet = new vscode.ThemeIcon('check');
    private static readonly unselectedSet: IconSet = new vscode.ThemeIcon('circle-slash');
    constructor(
        public readonly path: string,
        public readonly label: string,
        selected: boolean,
        public collapsibleState: vscode.TreeItemCollapsibleState,
        private readonly isOperation: boolean,
        public readonly children: OpenApiTreeNode[] = [],
        public readonly documentationUrl?: string,
    ) {
        super(label, collapsibleState);
        this.contextValue = documentationUrl;
        this.iconPath = selected ? OpenApiTreeNode.selectedSet : OpenApiTreeNode.unselectedSet;
    }
    public isNodeVisible(tokenizedFilter: string[]): boolean {
        if (tokenizedFilter.length === 0) {
            return true;
        }
        const lowerCaseSegment = this.label.toLowerCase();
        const splatPath = trimOperation(this.path);
        if (tokenizedFilter.some(x => lowerCaseSegment.includes(x.toLowerCase()))) {
            if (this.isOperation &&tokenizedFilter.some(x => operationsNames.has(x)) && !tokenizedFilter.some(x => splatPath.includes(x))) {
                return false;
            }
            return true;
        }
        
        if (this.isOperation) {
            const segments = getPathSegments(splatPath);
            if (segments.length === 0) {
                return false;
            }
            const parentSegment = segments[segments.length - 1];
            return tokenizedFilter.some(x => parentSegment.includes(x));
        }
        return this.children.some(x => x.isNodeVisible(tokenizedFilter));
    }
}