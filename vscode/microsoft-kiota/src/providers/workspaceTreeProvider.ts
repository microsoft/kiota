import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

import { KIOTA_WORKSPACE_FILE } from '../constants';
import { getWorkspaceJsonPath } from '../util';

interface WorkspaceContent {
  version: string;
  clients: Record<string, any>;
  plugins: Record<string, any>;
}

class WorkspaceTreeItem extends vscode.TreeItem {
  constructor(
    public readonly label: string,
    public readonly collapsibleState: vscode.TreeItemCollapsibleState,
    public readonly type: 'root' | 'category' | 'item',
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
      return [
        new WorkspaceTreeItem(KIOTA_WORKSPACE_FILE, vscode.TreeItemCollapsibleState.Expanded, 'root')
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
          new WorkspaceTreeItem(clientName, vscode.TreeItemCollapsibleState.None, 'item')
        );
      }

      if (element.label === 'Plugins') {
        return Object.keys(this.workspaceContent.plugins).map(pluginName =>
          new WorkspaceTreeItem(pluginName, vscode.TreeItemCollapsibleState.None, 'item')
        );
      }
    }
    return [];
  }

  getTreeItem(element: WorkspaceTreeItem): WorkspaceTreeItem {
    if (element) {
      if (element.type === 'root') {
        element.command = {
          command: 'kiota.workspace.openWorkspaceFile',
          title: vscode.l10n.t("Open File"),
          arguments: [vscode.Uri.file(getWorkspaceJsonPath())]
        };
        element.contextValue = 'folder';
      } else if (element.type === 'item') {
        element.contextValue = 'item';
        element.iconPath = new vscode.ThemeIcon('check');
        element.command = {
          command: 'kiota.workspace.playItem',
          title: vscode.l10n.t("Play Item"),
          arguments: [element.label]
        };
        element.contextValue = 'item';
      }
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
