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

export function getWorkspaceJsonDirectory(clientNameOrPluginName?: string): string {
  const baseDir = path.join(
    vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0
      ? vscode.workspace.workspaceFolders[0].uri.fsPath
      : process.env.HOME ?? process.env.USERPROFILE ?? process.cwd()
  );

  let workspaceFolder = !vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0
    ? path.join(baseDir, 'kiota', clientNameOrPluginName ?? '')
    : baseDir;

  if (!fs.existsSync(workspaceFolder)) {
    fs.mkdirSync(workspaceFolder, { recursive: true });
  }
  return workspaceFolder;
}

//used to store output in the App Package directory in the case where the workspace is a Teams Toolkit Project
export function findAppPackageDirectory(directory: string): string | null {
  if (!fs.existsSync(directory)) {
    return null;
  }

  const entries = fs.readdirSync(directory, { withFileTypes: true });

  for (const entry of entries) {
    const fullPath = path.join(directory, entry.name);

    if (entry.isDirectory()) {
      if (entry.name === 'appPackage') {
        return fullPath;
      }

      const subDirectory = findAppPackageDirectory(fullPath);
      if (subDirectory) {
        return subDirectory;
      }
    }
  }

  return null;
}

export async function findLockFile(directory: string): Promise<string | undefined> {
  return new Promise((resolve) => {
    fs.readdir(directory, (err, files) => {
      if (err) {
        resolve(undefined);
        return;
      }
      for (const file of files) {
        const fullPath = path.join(directory, file);
        
        if (file === 'kiota-lock.json') {
          resolve(fullPath);
          return;
        }
        
        // Check directories recursively
        if (fs.statSync(fullPath).isDirectory()) {
          void findLockFile(fullPath).then((result) => {
            if (result) {
              resolve(result);
              return;
            }
          });
        }
      }
      
      resolve(undefined);
    });
  });
}