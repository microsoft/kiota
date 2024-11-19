import * as vscode from 'vscode';

import { RegenerateCommand } from '../commands/regenerate/regenerateCommand';
import { CLIENTS, KIOTA_WORKSPACE_FILE, PLUGINS } from '../constants';
import { ClientOrPluginProperties } from '../kiotaInterop';
import { getWorkspaceJsonPath, isClientType, isKiotaWorkspaceFilePresent, isPluginType } from '../util';
import { SharedService } from './sharedService';

interface WorkspaceContent {
  version: string;
  clients: Record<string, ClientOrPluginProperties>;
  plugins: Record<string, ClientOrPluginProperties>;
}

export class WorkspaceTreeItem extends vscode.TreeItem {
  constructor(
    public readonly label: string,
    public readonly collapsibleState: vscode.TreeItemCollapsibleState,
    public readonly type: 'root' | 'category' | 'item',
    public readonly category?: string,
    public readonly properties?: ClientOrPluginProperties,
    public command?: vscode.Command
  ) {
    super(label, collapsibleState);
    this.contextValue = type;
  }
}

export class WorkspaceTreeProvider implements vscode.TreeDataProvider<WorkspaceTreeItem> {
  public isWorkspacePresent: boolean;
  private _onDidChangeTreeData: vscode.EventEmitter<WorkspaceTreeItem | undefined | null | void> = new vscode.EventEmitter<WorkspaceTreeItem | undefined | null | void>();
  readonly onDidChangeTreeData: vscode.Event<WorkspaceTreeItem | undefined | null | void> = this._onDidChangeTreeData.event;
  private workspaceContent: WorkspaceContent | null = null;
  private sharedService: SharedService;

  constructor(isWSPresent: boolean, _sharedService: SharedService) {
    this.isWorkspacePresent = isWSPresent;
    this.sharedService = _sharedService;
    void this.loadWorkspaceContent();
  }

  async refreshView(): Promise<void> {
    this._onDidChangeTreeData.fire();
  }

  async getChildren(element?: WorkspaceTreeItem): Promise<WorkspaceTreeItem[]> {
    if (!this.isWorkspacePresent) {
      return [];
    }

    if (!element) {
      const hasClients = this.workspaceContent?.clients && Object.keys(this.workspaceContent.clients).length > 0;
      const hasPlugins = this.workspaceContent?.plugins && Object.keys(this.workspaceContent.plugins).length > 0;
      const collapsibleState = (hasClients || hasPlugins) ? vscode.TreeItemCollapsibleState.Expanded : vscode.TreeItemCollapsibleState.Collapsed;
      return [
        new WorkspaceTreeItem(KIOTA_WORKSPACE_FILE, collapsibleState, 'root')
      ];
    }

    if (this.workspaceContent) {
      if (element.label === KIOTA_WORKSPACE_FILE) {
        const children: WorkspaceTreeItem[] = [];
        if (Object.keys(this.workspaceContent.clients).length > 0) {
          children.push(new WorkspaceTreeItem(CLIENTS, vscode.TreeItemCollapsibleState.Expanded, 'category'));
        }
        if (Object.keys(this.workspaceContent.plugins).length > 0) {
          children.push(new WorkspaceTreeItem(PLUGINS, vscode.TreeItemCollapsibleState.Expanded, 'category'));
        }
        return children;
      }

      if (isClientType(element.label)) {
        return Object.keys(this.workspaceContent.clients).map(clientName =>
          new WorkspaceTreeItem(clientName, vscode.TreeItemCollapsibleState.None, 'item', CLIENTS, this.getProperties(clientName, CLIENTS))
        );
      }

      if (isPluginType(element.label)) {
        return Object.keys(this.workspaceContent.plugins).map(pluginName =>
          new WorkspaceTreeItem(pluginName, vscode.TreeItemCollapsibleState.None, 'item', PLUGINS, this.getProperties(pluginName, CLIENTS))
        );
      }
    }
    return [];
  }

  getProperties(name: string, category: string): ClientOrPluginProperties | undefined {
    if (category && category === CLIENTS) {
      return this.workspaceContent?.plugins[name];
    }
    return this.workspaceContent?.clients[name];
  }

  getTreeItem(element: WorkspaceTreeItem): WorkspaceTreeItem {
    if (!element) {
      return element;
    }

    switch (element.type) {
      case 'root':
        element.command = {
          command: 'kiota.workspace.openWorkspaceFile',
          title: vscode.l10n.t("Open File"),
          arguments: [vscode.Uri.file(getWorkspaceJsonPath())]
        };
        element.contextValue = 'folder';
        break;

      case 'item':
        const key = element.label;
        const clientOrPluginKey = this.sharedService.get('clientOrPluginKey');
        element.iconPath = (clientOrPluginKey && clientOrPluginKey === key) ?
          new vscode.ThemeIcon('folder-opened') :
          new vscode.ThemeIcon('folder');
        break;
    }
    return element;
  }

  public loadWorkspaceContent(): void {
    if (!this.isWorkspacePresent) {
      this.workspaceContent = null;
      return;
    }
    try {
      const workspaceJson = vscode.workspace.textDocuments.find(doc => doc.fileName.endsWith(KIOTA_WORKSPACE_FILE));
      if (!workspaceJson) {
        throw new Error('Workspace file not found');
      }
      const content = workspaceJson.getText();
      this.workspaceContent = JSON.parse(content);
    } catch (error) {
      console.error('Error loading workspace.json:', error);
    }
  }
}

async function openResource(resource: vscode.Uri): Promise<void> {
  await vscode.window.showTextDocument(resource);
}

export async function loadTreeView(context: vscode.ExtensionContext, treeDataProvider: WorkspaceTreeProvider): Promise<void> {
  context.subscriptions.push(vscode.workspace.onDidChangeWorkspaceFolders(async () => {
    treeDataProvider.isWorkspacePresent = await isKiotaWorkspaceFilePresent();
    await vscode.commands.executeCommand('kiota.workspace.refresh'); // Refresh the tree view when workspace folders change
  }));
  context.subscriptions.push(vscode.window.createTreeView('kiota.workspace', { treeDataProvider }));
  context.subscriptions.push(vscode.commands.registerCommand('kiota.workspace.openWorkspaceFile', openResource));
  context.subscriptions.push(vscode.commands.registerCommand('kiota.workspace.refresh', async () => {
    treeDataProvider.isWorkspacePresent = await isKiotaWorkspaceFilePresent();
    await treeDataProvider.refreshView();
  }));
  context.subscriptions.push(
    vscode.commands.registerCommand('kiota.workspace.selectItem', async (workspaceTreeItem: WorkspaceTreeItem) => {
      const { label, properties, category } = workspaceTreeItem;
      await vscode.commands.executeCommand('kiota.editPaths', label, properties, category);
    })
  );

}
;