import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { KIOTA_DIRECTORY, KIOTA_WORKSPACE_FILE } from './constants';

export function getWorkspaceJsonPath(): string {
    return path.join(vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0 ? 
                                        vscode.workspace.workspaceFolders[0].uri.fsPath :
                                        process.env.HOME ?? process.env.USERPROFILE ?? process.cwd(), 
                                        KIOTA_DIRECTORY,
                                        KIOTA_WORKSPACE_FILE);
};

export class WorkspaceTreeProvider implements vscode.TreeDataProvider<vscode.TreeItem> {
    constructor(public isWSPresent: boolean) {
    }
    async getChildren(element?: vscode.TreeItem): Promise<vscode.TreeItem[]> {
        if (!element) {
            return [new vscode.TreeItem(KIOTA_WORKSPACE_FILE, vscode.TreeItemCollapsibleState.None)];
        }
        return [];
    }

    getTreeItem(element: vscode.TreeItem): vscode.TreeItem {
        if (element) {
            element.command = { command: 'kiota.workspace.openWorkspaceFile', title: vscode.l10n.t("Open File"), arguments: [vscode.Uri.file(getWorkspaceJsonPath())], };
            element.contextValue = 'file';
        }
        return element;
    }
}

async function openResource(resource: vscode.Uri): Promise<void> {
    const workspaceJsonPath = getWorkspaceJsonPath();
    try{
        await vscode.window.showTextDocument(resource);
    } catch (error) {
        const dirPath = path.dirname(workspaceJsonPath);
        await fs.promises.mkdir(dirPath, { recursive: true });
        await fs.promises.writeFile(workspaceJsonPath, Buffer.from('{}'));
        await vscode.window.showTextDocument(resource);
    }
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
    vscode.workspace.onDidChangeWorkspaceFolders(async () => {
        treeDataProvider.isWSPresent = await isKiotaWorkspaceFilePresent();
    });
    context.subscriptions.push(vscode.window.createTreeView('kiota.workspace', { treeDataProvider }));
    context.subscriptions.push(vscode.commands.registerCommand('kiota.workspace.openWorkspaceFile', openResource));
}
