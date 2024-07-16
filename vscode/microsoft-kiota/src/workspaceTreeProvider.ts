import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { KIOTA_WORKSPACE_FILE } from './constants';
import { getWorkspaceJsonPath } from './util';


export class WorkspaceTreeProvider implements vscode.TreeDataProvider<vscode.TreeItem> {
    constructor(public isWSPresent: boolean) {
    }
    async getChildren(element?: vscode.TreeItem): Promise<vscode.TreeItem[]> {
      if (!this.isWSPresent) {
        return [];
      }
      if (!element) {
        return [new vscode.TreeItem(KIOTA_WORKSPACE_FILE, vscode.TreeItemCollapsibleState.None)];
      }
      return [];
    }

    getTreeItem(element: vscode.TreeItem): vscode.TreeItem {
        if (element) {
            element.command = {
              command: 'kiota.workspace.openWorkspaceFile', 
              title: vscode.l10n.t("Open File"), 
              arguments: [vscode.Uri.file(getWorkspaceJsonPath())] 
            };
            element.contextValue = 'file';
        }
        return element;
    }
}

async function openResource(resource: vscode.Uri): Promise<void> {
        await vscode.window.showTextDocument(resource);
}
async function isKiotaWorkspaceFilePresent(): Promise<boolean> {
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
      treeDataProvider.isWSPresent = await isKiotaWorkspaceFilePresent();
      await vscode.commands.executeCommand('kiota.workspace.refresh'); // Refresh the tree view when workspace folders change
  }));
    context.subscriptions.push(vscode.window.createTreeView('kiota.workspace', { treeDataProvider }));
    context.subscriptions.push(vscode.commands.registerCommand('kiota.workspace.openWorkspaceFile', openResource));
    context.subscriptions.push(vscode.commands.registerCommand('kiota.workspace.refresh', async () => { 
      treeDataProvider.isWSPresent = await isKiotaWorkspaceFilePresent();
    }));
}
