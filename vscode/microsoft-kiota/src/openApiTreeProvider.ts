import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import * as rpc from 'vscode-jsonrpc/node';
import { 
    ClientObjectProperties, 
    ClientOrPluginProperties, 
    connectToKiota, 
    KiotaGetManifestDetailsConfiguration, 
    KiotaLogEntry, 
    KiotaManifestResult, 
    KiotaOpenApiNode, 
    KiotaShowConfiguration, 
    KiotaShowResult, 
    ConfigurationFile, 
    PluginObjectProperties } from './kiotaInterop';
import { ExtensionSettings } from './extensionSettings';
import { treeViewId } from './constants';
import { updateTreeViewIcons } from './util';

export class OpenApiTreeProvider implements vscode.TreeDataProvider<OpenApiTreeNode> {
    private _onDidChangeTreeData: vscode.EventEmitter<OpenApiTreeNode | undefined | null | void> = new vscode.EventEmitter<OpenApiTreeNode | undefined | null | void>();
    readonly onDidChangeTreeData: vscode.Event<OpenApiTreeNode | undefined | null | void> = this._onDidChangeTreeData.event;
    private apiTitle?: string;
    private selectionChanged: boolean = false;
    constructor(
        private readonly context: vscode.ExtensionContext,
        private readonly settingsGetter: () => ExtensionSettings,
        private _descriptionUrl?: string,
        public includeFilters: string[] = [],
        public excludeFilters: string[] = []) {

    }
    private _lockFilePath?: string;
    private _lockFile?: ConfigurationFile | Partial<ConfigurationFile> = {};
    public get isLockFileLoaded(): boolean {
        return !!this._lockFile;
    }
    public async loadLockFile(path: string, clientOrPluginName?: string): Promise<void> {
        this.closeDescription(false);
        this._lockFilePath = path;
        const lockFileData = await vscode.workspace.fs.readFile(vscode.Uri.file(path));
        let parsedLockFile = JSON.parse(lockFileData.toString()) as ConfigurationFile;

        if (clientOrPluginName) {
            let filteredData: Partial<ConfigurationFile> = { version: parsedLockFile.version };

            if (parsedLockFile.clients && parsedLockFile.clients[clientOrPluginName]) {
                filteredData.clients = {
                    [clientOrPluginName]: parsedLockFile.clients[clientOrPluginName]
                };
            }

            if (parsedLockFile.plugins && parsedLockFile.plugins[clientOrPluginName]) {
                filteredData.plugins = {
                    [clientOrPluginName]: parsedLockFile.plugins[clientOrPluginName]
                };
            }

            parsedLockFile = filteredData as ConfigurationFile;
        }

        this._lockFile = parsedLockFile;

        const clientOrPlugin: ClientOrPluginProperties | undefined =
            Object.values(this._lockFile.clients ?? {})[0] ||
            Object.values(this._lockFile.plugins ?? {})[0];

        if (clientOrPlugin) {
            this._descriptionUrl = clientOrPlugin.descriptionLocation;
            this.includeFilters = clientOrPlugin.includePatterns;
            this.excludeFilters = clientOrPlugin.excludePatterns;

            const settings = this.settingsGetter();
            await this.loadNodes(settings.clearCache, clientOrPluginName);

            if (this.rawRootNode) {
                this.refreshView();
            }
        }
    }
    public async loadEditPaths(clientOrPluginKey: string, clientObject: ClientOrPluginProperties): Promise<void> {
        this.closeDescription(false);
        const newLockFile: ConfigurationFile = { version: '1.0.0', clients: {}, plugins: {} };

        if ((clientObject as ClientObjectProperties).clientNamespaceName) {
            newLockFile.clients[clientOrPluginKey] = clientObject as ClientObjectProperties;
        } else {
            newLockFile.plugins[clientOrPluginKey] = clientObject as PluginObjectProperties;
        }
        this._lockFile = newLockFile;
        if (clientObject.descriptionLocation) {
            this._descriptionUrl = clientObject.descriptionLocation;
            this.includeFilters = clientObject.includePatterns;
            this.excludeFilters = clientObject.excludePatterns;

            const settings = this.settingsGetter();
            await this.loadNodes(settings.clearCache, clientOrPluginKey);

            if (this.rawRootNode) {
                this.refreshView();
            }
        }
    }
    public async loadManifestFromUri(path: string, apiIdentifier?: string): Promise<KiotaLogEntry[]> {
        this.closeDescription(false);
        const logs = await this.loadNodesFromManifest(path, apiIdentifier);
        if (this.rawRootNode) {
            this.refreshView();
        }
        return logs;
    }
    public async loadManifestFromContent(jsonManifest: string, apiIdentifier?: string): Promise<KiotaLogEntry[]> {
        this.closeDescription(false);
        const manifestFilePath = path.join(os.tmpdir(), "kiota-vscode-extension", Date.now().toString(), "manifest.json");
        fs.mkdirSync(path.parse(manifestFilePath).dir, { recursive: true });
        await vscode.workspace.fs.writeFile(vscode.Uri.file(manifestFilePath), Buffer.from(jsonManifest, 'utf8'));
        return await this.loadManifestFromUri(manifestFilePath, apiIdentifier);
    }
    private setAllSelected(node: KiotaOpenApiNode, selected: boolean) {
        node.selected = selected;
        node.children.forEach(x => this.setAllSelected(x, selected));
    }
    private getFirstClient(): ClientObjectProperties | undefined {
        return this._lockFile?.clients ? Object.values(this._lockFile.clients)[0] : undefined;
    }
    public get outputPath(): string {
        return this._lockFilePath ? path.parse(this._lockFilePath).dir : '';
    }
    public get clientClassName(): string {
        if (this._lockFile?.clients) {
            const client = this.getFirstClient();
            return client ? client.clientNamespaceName : '';
        }
        return '';
    }

    public get clientNamespaceName(): string {
        if (this._lockFile?.clients) {
            const client = this.getFirstClient();
            return client ? client.clientNamespaceName : '';
        }
        return '';
    }

    public get language(): string {
        if (this._lockFile?.clients) {
            const client = this.getFirstClient();
            return client ? client.language : '';
        }
        return '';
    }
    public closeDescription(shouldRefresh = true) {
        this._descriptionUrl = '';
        this.rawRootNode = undefined;
        this._lockFile = undefined;
        this._lockFilePath = undefined;
        this.tokenizedFilter = [];
        this._filterText = '';
        this.includeFilters = [];
        this.excludeFilters = [];
        if (shouldRefresh) {
            this.refreshView();
        }
        void updateTreeViewIcons(treeViewId, false);
    }
    public isEmpty(): boolean {
        return this.rawRootNode === undefined;
    }
    public hasSelectionChanged(): boolean {
        return this.selectionChanged;
    }
    public setSelectionChanged(state: boolean) {
        this.selectionChanged = state;
    }
    public async setDescriptionUrl(descriptionUrl: string): Promise<void> {
        this.closeDescription(false);
        this._descriptionUrl = descriptionUrl;
        const settings = this.settingsGetter();
        await this.loadNodes(settings.clearCache);
        this.refreshView();
    }
    public get descriptionUrl(): string {
        return this._descriptionUrl ?? '';
    }
    public select(item: OpenApiTreeNode, selected: boolean, recursive: boolean): void {
        if (!this.rawRootNode) {
            return;
        }
        const apiNode = this.findApiNode(getPathSegments(item.path), this.rawRootNode);
        if (apiNode) {
            this.selectInternal(apiNode, selected, recursive);
            this.refreshView();
        }
    }
    private selectInternal(apiNode: KiotaOpenApiNode, selected: boolean, recursive: boolean) {
        apiNode.selected = selected;
        this.setSelectionChanged(true);
        console.log("selected:", this.hasSelectionChanged())
        const isOperationNode = apiNode.isOperation ?? false;
        if (recursive) {
            apiNode.children.forEach(x => this.selectInternal(x, selected, recursive));
        } else if (!isOperationNode) {
            apiNode.children.filter(x => x.isOperation ?? false).forEach(x => this.selectInternal(x, selected, false));
        } else if (isOperationNode && !selected && this.rawRootNode) {
            const parent = this.findApiNode(getPathSegments(trimOperation(apiNode.path)), this.rawRootNode);
            if (parent) {
                parent.selected = selected;
                this.setSelectionChanged(true);
            }
        }
    }
    private findApiNode(segments: string[], currentNode: KiotaOpenApiNode): KiotaOpenApiNode | undefined {
        if (segments.length === 0) {
            return currentNode;
        }
        const segment = segments.shift();
        if (segment) {
            let child: KiotaOpenApiNode | undefined;
            if (currentNode.clientNameOrPluginName) {
                const rootChild = currentNode.children.find(x => x.segment === '/');
                if (rootChild) {
                    child = rootChild.children.find(x => x.segment === segment);
                }
            } else {
                child = currentNode.children.find(x => x.segment === segment);
            }
            if (child) {
                return this.findApiNode(segments, child);
            } else if (segment.startsWith('{') && segment.endsWith('}')) {
                // in case there are multiple single parameters nodes with different names at the same level
                child = currentNode.children.find(x => x.segment.startsWith('{') && x.segment.endsWith('}'));
                if (child) {
                    return this.findApiNode(segments, child);
                }
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
        if (currentNode.selected || false) {
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
        this.tokenizedFilter = filterText.length === 0 ? [] : filterText.split(' ').filter(x => x !== '').map(x => x.trim().toLowerCase()).sort();
        this.refreshView();
    }
    public get filter(): string {
        return this._filterText;
    }
    private async loadNodesFromManifest(manifestPath: string, apiIdentifier?: string): Promise<KiotaLogEntry[]> {
        const settings = this.settingsGetter();
        const result = await connectToKiota(this.context, async (connection) => {
            const request = new rpc.RequestType<KiotaGetManifestDetailsConfiguration, KiotaManifestResult, void>('GetManifestDetails');
            return await connection.sendRequest(request, {
                manifestPath,
                apiIdentifier: apiIdentifier ?? '',
                clearCache: settings.clearCache
            });
        });
        if (result) {
            this._descriptionUrl = result.apiDescriptionPath;
            this.includeFilters = result.selectedPaths ?? [];
            await this.loadNodes(settings.clearCache);
            return result.logs;
        }
        return [];
    }
    private async loadNodes(clearCache: boolean, clientNameOrPluginName?: string): Promise<void> {
        if (!this.descriptionUrl || this.descriptionUrl.length === 0) {
            return;
        }
        const result = await connectToKiota(this.context, async (connection) => {
            const request = new rpc.RequestType<KiotaShowConfiguration, KiotaShowResult, void>('Show');
            return await connection.sendRequest(request, {
                includeFilters: this.includeFilters,
                excludeFilters: this.excludeFilters,
                descriptionPath: this.descriptionUrl,
                clearCache
            });
        });
        if (result) {
            this.apiTitle = result.apiTitle;
            if (result.rootNode) {
                if (this.includeFilters.length === 0) {
                    this.setAllSelected(result.rootNode, false);
                }
                this.rawRootNode = result.rootNode;
                if (clientNameOrPluginName) {
                    this.rawRootNode = createKiotaOpenApiNode(
                        clientNameOrPluginName,
                        '/',
                        [this.rawRootNode],
                        false,
                        false,
                        undefined,
                        clientNameOrPluginName
                    );
                }
               await updateTreeViewIcons(treeViewId, true, false);
            }
        }
    }
    getCollapsedState(node: KiotaOpenApiNode): vscode.TreeItemCollapsibleState {
        return node.children.length === 0 ?
            vscode.TreeItemCollapsibleState.None :
            (this.tokenizedFilter.length === 0 ?
                vscode.TreeItemCollapsibleState.Collapsed :
                vscode.TreeItemCollapsibleState.Expanded);
    }
    getTreeNodeFromKiotaNode(node: KiotaOpenApiNode, collapsibleStateOverride: vscode.TreeItemCollapsibleState | undefined = undefined): OpenApiTreeNode {
        return new OpenApiTreeNode(
            node.path,
            node.segment === pathSeparator && this.apiTitle ? pathSeparator + " (" + this.apiTitle + ")" : node.segment,
            node.selected ?? false,
            collapsibleStateOverride ?? this.getCollapsedState(node),
            node.isOperation ?? false,
            this.tokenizedFilter,
            this.apiTitle,
            node.children.map(x => this.getTreeNodeFromKiotaNode(x)),
            node.documentationUrl,
            node.clientNameOrPluginName
        );
    }
    getChildren(element?: OpenApiTreeNode): vscode.ProviderResult<OpenApiTreeNode[]> {
        if (!this.rawRootNode) {
            return [];
        }
        if (element) {
            return element.children.filter(x => x.isNodeVisible(this.tokenizedFilter));
        } else {
            const result = this.getTreeNodeFromKiotaNode(this.rawRootNode, vscode.TreeItemCollapsibleState.Expanded);
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

function createKiotaOpenApiNode(
    segment: string,
    path: string,
    children: KiotaOpenApiNode[] = [],
    selected?: boolean,
    isOperation?: boolean,
    documentationUrl?: string,
    clientNameOrPluginName?: string
): KiotaOpenApiNode {
    return {
        segment,
        path,
        children,
        selected,
        isOperation,
        documentationUrl,
        clientNameOrPluginName
    };
}
type IconSet = string | vscode.Uri | { light: string | vscode.Uri; dark: string | vscode.Uri } | vscode.ThemeIcon;
export class OpenApiTreeNode extends vscode.TreeItem {
    private static readonly selectedSet: IconSet = new vscode.ThemeIcon('check');
    private static readonly unselectedSet: IconSet = new vscode.ThemeIcon('circle-slash');

    constructor(
        public readonly path: string,
        public readonly label: string,
        selected: boolean,
        public readonly collapsibleState: vscode.TreeItemCollapsibleState,
        private readonly isOperation: boolean,
        filterTokens: string[],
        apiTitle: string | undefined,
        public readonly children: OpenApiTreeNode[] = [],
        public readonly documentationUrl?: string,
        public readonly clientNameOrPluginName?: string
    ) {
        super(label, collapsibleState);
        this.id = `${path}_${filterTokens.join('_')}`; // so the collapsed state is NOT persisted between filter changes
        this.contextValue = label === pathSeparator + " (" + apiTitle + ")" ? 'apiTitle' : (this.documentationUrl ? 'documentationUrl' : '');
        this.iconPath = selected ? OpenApiTreeNode.selectedSet : OpenApiTreeNode.unselectedSet;
        if (clientNameOrPluginName) {
            this.label = clientNameOrPluginName;
            this.contextValue = 'clientNameOrPluginName';
        }
    }
    public isNodeVisible(tokenizedFilter: string[]): boolean {
        if (tokenizedFilter.length === 0) {
            return true;
        }
        const lowerCaseSegment = this.label.toLowerCase();
        const splatPath = trimOperation(this.path);
        if (tokenizedFilter.some(x => lowerCaseSegment.includes(x.toLowerCase()))) {
            if (this.isOperation && tokenizedFilter.some(x => operationsNames.has(x)) && !tokenizedFilter.some(x => splatPath.includes(x))) {
                return false;
            }
            return true;
        }

        const segments = getPathSegments(splatPath);

        return tokenizedFilter.some(x => segments.some(s => s.includes(x)))
            || this.children.some(x => x.isNodeVisible(tokenizedFilter));
    }
}