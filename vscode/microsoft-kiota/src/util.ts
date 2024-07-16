import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { APIMANIFEST, CLIENT, CLIENTS, KIOTA_DIRECTORY, KIOTA_WORKSPACE_FILE, PLUGIN, PLUGINS } from './constants';

const clientTypes = [CLIENT, CLIENTS];
const pluginTypes = [PLUGIN, PLUGINS, APIMANIFEST];

export function isClientType(type: string): boolean {
  return clientTypes.includes(type);
}

export function isPluginType(type: string): boolean {
  return pluginTypes.includes(type);
}

export async function updateTreeViewIcons(treeViewId: string, showIcons: boolean, showRegenerateIcon?: boolean) {
    await vscode.commands.executeCommand('setContext', `${treeViewId}.showIcons`, showIcons);
    if (showRegenerateIcon !== undefined) {
        await vscode.commands.executeCommand('setContext', `${treeViewId}.showRegenerateIcon`, showRegenerateIcon);
    }
}

export function getWorkspaceJsonPath(): string {
    return path.join(getWorkspaceJsonDirectory(),KIOTA_DIRECTORY, KIOTA_WORKSPACE_FILE);
};

export function getWorkspaceJsonDirectory(): string {
  const baseDir = path.join(vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0 ?
  vscode.workspace.workspaceFolders[0].uri.fsPath :
  process.env.HOME ?? process.env.USERPROFILE ?? process.cwd());
  const workspaceFolder = !vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0 ? 
  path.join(baseDir, 'kiota') : baseDir ;
  if (!fs.existsSync(workspaceFolder)) {
    fs.mkdirSync(workspaceFolder, { recursive: true });
}
  return workspaceFolder;
}