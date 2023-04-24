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
                    const currentNode = this.findApiNode(ip.split('/').filter(x => x !== ''), this.rawRootNode!);
                    if(currentNode) {
                        currentNode.selected = true;
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
        const apiNode = this.findApiNode(this.getPathSegments(item.path), this.rawRootNode);
        if(apiNode) {
            this.selectInternal(apiNode, selected, recursive);
            this.refreshView();
        }
    }
    private selectInternal(apiNode: KiotaOpenApiNode, selected: boolean, recursive: boolean) {
        apiNode.selected = selected;
        if(recursive) {
            apiNode.children.forEach(x => this.selectInternal(x, selected, recursive));
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
        return this.findSelectedPaths(this.rawRootNode).map(x => x === '' ? '/' : x); // root node trailing slash is /
    }
    private findSelectedPaths(currentNode: KiotaOpenApiNode): string[] {
        const result: string[] = [];
        if(currentNode.selected || false) {
            result.push(currentNode.path.replace(/\\/g, '/'));
        }
        currentNode.children.forEach(x => result.push(...this.findSelectedPaths(x)));
        return result;
    }
    private getPathSegments(path: string): string[] {
        return path.replace('/', '').split('\\').filter(x => x !== ''); // the root node is always /
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
        const result = new OpenApiTreeNode(
            node.path, 
            node.segment,
            node.selected || false,
            this.getCollapsedState(node.children.length > 0)
        );
        result.children = node.children.map(x => this.getTreeNodeFromKiotaNode(x));
        return result;
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
type IconSet = string | vscode.Uri | { light: string | vscode.Uri; dark: string | vscode.Uri } | vscode.ThemeIcon;
export class OpenApiTreeNode extends vscode.TreeItem {
    private static readonly selectedSet: IconSet = new vscode.ThemeIcon('check');
    private static readonly unselectedSet: IconSet = new vscode.ThemeIcon('circle-slash');
    public children: OpenApiTreeNode[];
    constructor(
        public readonly path: string,
        public readonly label: string,
        selected: boolean,
        public collapsibleState: vscode.TreeItemCollapsibleState,
        _children?: OpenApiTreeNode[]
    ) {
        super(label, collapsibleState);
        this.iconPath = selected ? OpenApiTreeNode.selectedSet : OpenApiTreeNode.unselectedSet;
        if (_children) {
            this.children = _children;
        } else {
            this.children = [];
        }
    }
    public isNodeVisible(tokenizedFilter: string[]): boolean {
        if (tokenizedFilter.length === 0) {
            return true;
        }
        const lowerCaseSegment = this.label.toLowerCase();
        if (tokenizedFilter.some(x => lowerCaseSegment.includes(x.toLowerCase()))) {
            return true;
        }
        return this.children.some(x => x.isNodeVisible(tokenizedFilter));
    }
}