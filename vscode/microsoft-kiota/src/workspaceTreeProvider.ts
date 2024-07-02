import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { KIOTA_DIRECTORY, KIOTA_WORKSPACE_FILE } from './constants';

const workspaceJsonPath = path.join(vscode.workspace.workspaceFolders?.map(folder => folder.uri.fsPath).join('') || '', KIOTA_DIRECTORY, KIOTA_WORKSPACE_FILE);

export class WorkspaceTreeProvider implements vscode.TreeDataProvider<vscode.TreeItem> {

    constructor(private context: vscode.ExtensionContext) {
        void this.ensureKiotaDirectory();
    }
    async getChildren(element?: vscode.TreeItem): Promise<vscode.TreeItem[]> {
        if (!element) {
            return [new vscode.TreeItem(KIOTA_WORKSPACE_FILE, vscode.TreeItemCollapsibleState.None)];
        }
        return [];
    }

    getTreeItem(element: vscode.TreeItem): vscode.TreeItem {
        if (element) {
            element.command = { command: 'kiota.workspace.openWorkspaceFile', title: vscode.l10n.t("Open File"), arguments: [vscode.Uri.file(workspaceJsonPath)], };
            element.contextValue = 'file';
        }
        return element;
    }

    private async ensureKiotaDirectory(): Promise<boolean> {
        const kiotaDir = path.dirname(workspaceJsonPath);
        try {
            await fs.promises.access(kiotaDir);
            return true;
        } catch (error) {
            return false;
        }
    }
}

export class KiotaWorkspace {
    constructor(context: vscode.ExtensionContext) {
        const treeDataProvider = new WorkspaceTreeProvider(context);
        context.subscriptions.push(vscode.window.createTreeView('kiota.workspace', { treeDataProvider }));
        vscode.commands.registerCommand('kiota.workspace.openWorkspaceFile', async (resource) => await this.openResource(resource));
    }
    private async openResource(resource: vscode.Uri): Promise<void> {
        try{
            await vscode.window.showTextDocument(resource);
        } catch (error) {
            await fs.promises.writeFile(workspaceJsonPath, Buffer.from('{}'));
            await vscode.window.showTextDocument(resource);
        }
        
    }
}