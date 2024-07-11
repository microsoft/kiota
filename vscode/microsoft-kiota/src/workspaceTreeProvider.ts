import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { KIOTA_DIRECTORY, KIOTA_WORKSPACE_FILE } from './constants';

const workspaceJsonPath = path.join(vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0 ? 
                                        vscode.workspace.workspaceFolders[0].uri.fsPath :
                                        '~/', 
                                        KIOTA_DIRECTORY,
                                        KIOTA_WORKSPACE_FILE);

export class WorkspaceTreeProvider implements vscode.TreeDataProvider<vscode.TreeItem> {
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
}

async function openResource(resource: vscode.Uri): Promise<void> {
    try{
        await vscode.window.showTextDocument(resource);
    } catch (error) {
        await fs.promises.writeFile(workspaceJsonPath, Buffer.from('{}'));
        await vscode.window.showTextDocument(resource);
    }
}
async function ensureKiotaDirectory(): Promise<void> {
    if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
        return;
    }
    const kiotaDir = path.dirname(workspaceJsonPath);
    try {
        await fs.promises.access(kiotaDir);
    } catch (error) {
        await vscode.window.showErrorMessage(
            vscode.l10n.t("Kiota directory not found")
        );
    }
}

export async function loadTreeView(context: vscode.ExtensionContext): Promise<void> {
    await ensureKiotaDirectory();
    const treeDataProvider = new WorkspaceTreeProvider();
    context.subscriptions.push(vscode.window.createTreeView('kiota.workspace', { treeDataProvider }));
    context.subscriptions.push(vscode.commands.registerCommand('kiota.workspace.openWorkspaceFile', async (resource) => await openResource(resource)));
}
