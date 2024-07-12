import * as vscode from 'vscode';
import * as os from 'os';
import * as path from 'path';
import { APIMANIFEST, CLIENT, CLIENTS, PLUGIN, PLUGINS } from './constants';

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

export function getKiotaWorkspacePath(): string {
    return vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0
      ? vscode.workspace.workspaceFolders[0].uri.fsPath
      : path.join(os.homedir(), 'kiota');
  }