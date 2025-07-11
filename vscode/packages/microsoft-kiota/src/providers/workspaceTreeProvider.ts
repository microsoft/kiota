import * as vscode from 'vscode';
import { ClientOrPluginProperties } from '@microsoft/kiota';

import { CLIENTS, KIOTA_WORKSPACE_FILE, PLUGINS } from '../constants';
import { WorkspaceContent, WorkspaceContentService } from '../modules/workspace';
import { getWorkspaceJsonPath, isClientType, isPluginType } from '../util';
import { SharedService } from './sharedService';

export class WorkspaceTreeItem extends vscode.TreeItem {
  constructor(
    public readonly label: string,
    public readonly collapsibleState: vscode.TreeItemCollapsibleState,
    public readonly type: 'root' | 'category' | 'item' | 'info',
    public readonly category?: string,
    public readonly properties?: ClientOrPluginProperties,
    public command?: vscode.Command
  ) {
    super(label, collapsibleState);
    this.contextValue = type;
  }
}

export class WorkspaceTreeProvider implements vscode.TreeDataProvider<WorkspaceTreeItem> {
  private _onDidChangeTreeData: vscode.EventEmitter<WorkspaceTreeItem | undefined | null | void> = new vscode.EventEmitter<WorkspaceTreeItem | undefined | null | void>();
  readonly onDidChangeTreeData: vscode.Event<WorkspaceTreeItem | undefined | null | void> = this._onDidChangeTreeData.event;
  private workspaceContent: WorkspaceContent | undefined = undefined;

  constructor(
    private workspaceContentService: WorkspaceContentService,
    private sharedService: SharedService
  ) {
  }

  async refreshView(): Promise<void> {
    await this.loadContent();
    this._onDidChangeTreeData.fire();
  }

  async loadContent(): Promise<void> {
    this.workspaceContent = await this.workspaceContentService.load();
  }

  async getChildren(element?: WorkspaceTreeItem): Promise<WorkspaceTreeItem[]> {
    if (!this.workspaceContent) {
      return [];
    }

    if (!element) {
      const hasClients = this.workspaceContent.clients && Object.keys(this.workspaceContent.clients).length > 0;
      const hasPlugins = this.workspaceContent.plugins && Object.keys(this.workspaceContent.plugins).length > 0;
      const hasWorkspaceContent = hasClients || hasPlugins;
      return hasWorkspaceContent ? [
        new WorkspaceTreeItem(KIOTA_WORKSPACE_FILE,
          vscode.TreeItemCollapsibleState.Expanded, 'root')
      ] : [
        new WorkspaceTreeItem(vscode.l10n.t('No clients or plugins are available'),
          vscode.TreeItemCollapsibleState.None, 'info')
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
          new WorkspaceTreeItem(pluginName, vscode.TreeItemCollapsibleState.None, 'item', PLUGINS, this.getProperties(pluginName, PLUGINS))
        );
      }
    }
    return [];
  }

  getProperties(name: string, category: string): ClientOrPluginProperties | undefined {
    if (category && category === CLIENTS) {
      return this.workspaceContent?.clients[name];
    }
    return this.workspaceContent?.plugins[name];
  }

  getTreeItem(element: WorkspaceTreeItem): WorkspaceTreeItem {
    if (!element) {
      return element;
    }

    switch (element.type) {
      case 'root':
        element.command = {
          command: 'kiota.workspace.openWorkspaceFile',
          title: vscode.l10n.t('Open File'),
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

}

async function openResource(resource: vscode.Uri): Promise<void> {
  await vscode.window.showTextDocument(resource);
}

export async function loadTreeView(context: vscode.ExtensionContext, treeDataProvider: WorkspaceTreeProvider): Promise<void> {
  context.subscriptions.push(vscode.workspace.onDidChangeWorkspaceFolders(async () => {
    await vscode.commands.executeCommand('kiota.workspace.refresh'); // Refresh the tree view when workspace folders change
  }));
  context.subscriptions.push(vscode.window.createTreeView('kiota.workspace', { treeDataProvider }));
  context.subscriptions.push(vscode.commands.registerCommand('kiota.workspace.openWorkspaceFile', openResource));
  context.subscriptions.push(vscode.commands.registerCommand('kiota.workspace.refresh', async () => {
    await treeDataProvider.refreshView();
  }));
  context.subscriptions.push(
    vscode.commands.registerCommand('kiota.workspace.selectItem', async (workspaceTreeItem: WorkspaceTreeItem) => {
      const { label, properties, category } = workspaceTreeItem;
      await vscode.commands.executeCommand('kiota.editPaths', label, properties, category);
    })
  );
  await vscode.commands.executeCommand('kiota.workspace.refresh');
};