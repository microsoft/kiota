import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { KIOTA_DIRECTORY, KIOTA_WORKSPACE_FILE } from './constants';

const workspaceJsonPath = path.join(vscode.workspace.workspaceFolders?.map(folder => folder.uri.fsPath).join('') || '', KIOTA_DIRECTORY, KIOTA_WORKSPACE_FILE);

export class WorkspaceTreeProvider implements vscode.TreeDataProvider<vscode.TreeItem> {

    private fileExists: boolean = false;

    constructor(private context: vscode.ExtensionContext) {
        void this.ensureKiotaDirectory();
    }
    async getChildren(element?: vscode.TreeItem): Promise<vscode.TreeItem[]> {
        if (!element) {
            try {
                await fs.promises.access(workspaceJsonPath);
                this.fileExists = true;
            } catch {
                this.fileExists = false;
            }
            const treeItem = new vscode.TreeItem(KIOTA_WORKSPACE_FILE, vscode.TreeItemCollapsibleState.None);
            treeItem.iconPath = vscode.ThemeIcon.File;
            if (!this.fileExists) {
                treeItem.label = `${KIOTA_WORKSPACE_FILE} (not found)`;
                treeItem.contextValue = 'file-not-found';
                treeItem.iconPath = new vscode.ThemeIcon('warning'); 
            } else {
                const fileExtension = path.extname(KIOTA_WORKSPACE_FILE);
                switch (fileExtension) {
                    case '.json':
                        treeItem.iconPath = new vscode.ThemeIcon('json'); 
                        break;
                    default:
                        treeItem.iconPath = vscode.ThemeIcon.File;
                }
            }
            return [treeItem];
        }
        return [];
    }

    getTreeItem(element: vscode.TreeItem): vscode.TreeItem {
        if (this.fileExists) {
            element.command = { 
                command: 'kiota.workspace.openWorkspaceFile', 
                title: vscode.l10n.t("Open File"), 
                arguments: [vscode.Uri.file(workspaceJsonPath)] 
            };
            element.contextValue = 'file';
        }
        return element;
    }

    private async ensureKiotaDirectory(): Promise<unknown> {
        if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
            await vscode.window.showErrorMessage(
                vscode.l10n.t("No workspace folder found, open a folder first")
            );
            return;
        }
        const kiotaDir = path.dirname(workspaceJsonPath);
        await fs.promises.access(kiotaDir).catch(() => {});
    }
}

export class KiotaWorkspace {
    constructor(context: vscode.ExtensionContext) {
        const treeDataProvider = new WorkspaceTreeProvider(context);
        context.subscriptions.push(vscode.window.createTreeView('kiota.workspace', { treeDataProvider }));
        vscode.commands.registerCommand('kiota.workspace.openWorkspaceFile', async (resource) => await this.openResource(resource));
    }

    private async openResource(resource: vscode.Uri): Promise<void> {
        try {
            await vscode.window.showTextDocument(resource);
        } catch (error) {
            await fs.promises.writeFile(workspaceJsonPath, Buffer.from('{}'));
            await vscode.window.showTextDocument(resource);
        }
    }
}
