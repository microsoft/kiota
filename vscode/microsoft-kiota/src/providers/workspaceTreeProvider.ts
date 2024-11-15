import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

import { KIOTA_WORKSPACE_FILE } from '../constants';
import { getWorkspaceJsonPath } from '../util';
import { ClientOrPluginProperties } from '../kiotaInterop';

interface WorkspaceContent {
  version: string;
  clients: Record<string, ClientOrPluginProperties>;
  plugins: Record<string, ClientOrPluginProperties>;
}

class WorkspaceTreeItem extends vscode.TreeItem {
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

export class WorkspaceTreeProvider implements vscode.TreeDataProvider<vscode.TreeItem> {
  public isWorkspacePresent: boolean;
  private _onDidChangeTreeData: vscode.EventEmitter<vscode.TreeItem | undefined | null | void> = new vscode.EventEmitter<vscode.TreeItem | undefined | null | void>();
  readonly onDidChangeTreeData: vscode.Event<vscode.TreeItem | undefined | null | void> = this._onDidChangeTreeData.event;
  private workspaceContent: WorkspaceContent | null = null;

  constructor(isWSPresent: boolean) {
    this.isWorkspacePresent = isWSPresent;
    void this.loadWorkspaceContent();
  }

  async refreshView(): Promise<void> {
    this.loadWorkspaceContent();
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
        return [
          new WorkspaceTreeItem('Clients', Object.keys(this.workspaceContent.clients).length > 0 ? vscode.TreeItemCollapsibleState.Expanded : vscode.TreeItemCollapsibleState.Collapsed, 'category'),
          new WorkspaceTreeItem('Plugins', Object.keys(this.workspaceContent.plugins).length > 0 ? vscode.TreeItemCollapsibleState.Expanded : vscode.TreeItemCollapsibleState.Collapsed, 'category')
        ];
      }

      if (element.label === 'Clients') {
        return Object.keys(this.workspaceContent.clients).map(clientName =>
          new WorkspaceTreeItem(clientName, vscode.TreeItemCollapsibleState.None, 'item', 'Clients', this.getProperties(clientName, 'Clients'))
        );
      }

      if (element.label === 'Plugins') {
        return Object.keys(this.workspaceContent.plugins).map(pluginName =>
          new WorkspaceTreeItem(pluginName, vscode.TreeItemCollapsibleState.None, 'item', 'Plugins', this.getProperties(pluginName, 'Plugins'))
        );
      }
    }
    return [];
  }

  getProperties(name: string, category: string): ClientOrPluginProperties | undefined {
    if (category && category === 'Plugins') {
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
        const properties = element.properties;
        const generationType = element.category;

        element.iconPath = new vscode.ThemeIcon('folder');
        element.command = {
          command: 'kiota.editPaths',
          title: vscode.l10n.t("Select"),
          arguments: [key, properties, generationType]
        };
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

async function isKiotaWorkspaceFilePresent(): Promise<boolean> {
  if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
    return false;
  }
  const workspaceFileDir = path.resolve(getWorkspaceJsonPath());
  try {
    await fs.promises.access(workspaceFileDir);
  } catch (error) {
    return false;
  }
  return true;
}

export async function loadTreeView(context: vscode.ExtensionContext): Promise<void> {
  const treeDataProvider = new WorkspaceTreeProvider(await isKiotaWorkspaceFilePresent());
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
}
