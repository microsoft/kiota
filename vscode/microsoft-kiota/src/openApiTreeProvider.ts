import * as path from 'path';
import * as vscode from 'vscode';
import * as rpc from 'vscode-jsonrpc/node';
import { connectToKiota, KiotaOpenApiNode, KiotaShowConfiguration, KiotaShowResult, LockFile } from './kiotaInterop';

export class OpenApiTreeProvider implements vscode.TreeDataProvider<OpenApiTreeNode> {
    private _onDidChangeTreeData: vscode.EventEmitter<OpenApiTreeNode | undefined | null | void> = new vscode.EventEmitter<OpenApiTreeNode | undefined | null | void>();
    readonly onDidChangeTreeData: vscode.Event<OpenApiTreeNode | undefined | null | void> = this._onDidChangeTreeData.event;
    constructor(private _descriptionUrl?: string,
        public readonly includeFilters: string[] = [],
        public readonly excludeFilters: string[] = []) {
        
    }
    private _lockFilePath?: string;
    private _lockFile?: LockFile;
    public async loadLockFile(path: string): Promise<void> {
      this._lockFilePath = path;
      const lockFileData = await vscode.workspace.fs.readFile(vscode.Uri.file(path));
      this._lockFile = JSON.parse(lockFileData.toString()) as LockFile;
      if (this._lockFile?.descriptionLocation) {
        this.descriptionUrl = this._lockFile.descriptionLocation;
        this.rawRootNode = undefined;
        await this.getChildren();
        if (this.rawRootNode) {
            this._lockFile.includePatterns.forEach(ip => {
                const currentNode = this.findApiNode(ip.split('/').filter(x => x !== ''), this.rawRootNode!);
                if(currentNode) {
                    currentNode.selected = true;
                }
            });
            this.refresh();
        }
      }
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
    public closeDescription() {
        this.descriptionUrl = '';
        this.rawRootNode = undefined;
        this.refresh();
    }
    public set descriptionUrl(descriptionUrl: string) {
        this._descriptionUrl = descriptionUrl;
        this.rawRootNode = undefined;
        this.refresh();
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
            this.refresh();
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

    refresh(): void {
        this._onDidChangeTreeData.fire();
    }
    getTreeItem(element: OpenApiTreeNode): vscode.TreeItem {
        return element;
    }
    public getSelectedPaths(): string[] {
        if (!this.rawRootNode) {
            return [];
        }
        return this.findSelectedPaths(this.rawRootNode);
    }
    private findSelectedPaths(currentNode: KiotaOpenApiNode): string[] {
        const result: string[] = [];
        if(currentNode.selected) {
            result.push(currentNode.path.replace(/\\/g, '/'));
        }
        currentNode.children.forEach(x => result.push(...this.findSelectedPaths(x)));
        return result;
    }
    private getPathSegments(path: string): string[] {
        return path.replace('/', '').split('\\').filter(x => x !== ''); // the root node is always /
    }
    private readonly selectedSet: IconSet = {
        light: path.join(__filename, '..', '..', 'media', 'light', 'ic_fluent_checkbox_checked_24_filled.svg'),
        dark: path.join(__filename, '..', '..', 'media', 'dark', 'ic_fluent_checkbox_checked_24_filled.svg')
    };
    private readonly unselectedSet: IconSet = {
        light: path.join(__filename, '..', '..', 'media', 'light', 'ic_fluent_checkbox_unchecked_24_filled.svg'),
        dark: path.join(__filename, '..', '..', 'media', 'dark', 'ic_fluent_checkbox_unchecked_24_filled.svg')
    };
    private getIconSet(selected: boolean): IconSet {
        return selected ? this.selectedSet : this.unselectedSet;
    }
    private rawRootNode: KiotaOpenApiNode | undefined;
    async getChildren(element?: OpenApiTreeNode): Promise<OpenApiTreeNode[]> {
        if (!this.descriptionUrl || this.descriptionUrl.length === 0) {
            return [];
        }
        if (!this.rawRootNode) {
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
            }
            else {
                return [];
            }
        }
        
        if (element) {
            return this.findChildren(this.getPathSegments(element.path), this.rawRootNode)
                        .map(x => new OpenApiTreeNode(x.path, 
                                                        x.segment, 
                                                        x.selected,
                                                        this.getIconSet(x.selected),
                                                        x.children.length > 0 ? vscode.TreeItemCollapsibleState.Collapsed : vscode.TreeItemCollapsibleState.None,
                                                    ));
        } else {
            return [new OpenApiTreeNode(this.rawRootNode.path, 
                                        this.rawRootNode.segment,
                                        this.rawRootNode.selected,
                                        this.getIconSet(this.rawRootNode.selected),
                                        vscode.TreeItemCollapsibleState.Expanded)];
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
type IconSet = { light: string | vscode.Uri; dark: string | vscode.Uri };

export class OpenApiTreeNode extends vscode.TreeItem {
    constructor(
        public readonly path: string,
        public readonly label: string,
        public selected: boolean,
        iconSet: IconSet,
        public readonly collapsibleState: vscode.TreeItemCollapsibleState,
        public readonly command?: vscode.Command
    ) {
        super(label, collapsibleState);
        this.iconPath = iconSet;
    }
}