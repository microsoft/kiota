
import * as vscode from 'vscode';

export function isPathInWorkspace(filePath: string): boolean {
  const workspaceFolders = vscode.workspace.workspaceFolders;
  if (!workspaceFolders) {
      return false;
  }

  return workspaceFolders.some(folder => filePath.startsWith(folder.uri.fsPath));
}