import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { APIMANIFEST, CLIENT, CLIENTS, KIOTA_DIRECTORY, KIOTA_WORKSPACE_FILE, PLUGIN, PLUGINS } from './constants';
import { migrateFromLockFile, displayMigrationMessages } from './migrateFromLockFile';

const clientTypes = [CLIENT, CLIENTS];
const pluginTypes = [PLUGIN, PLUGINS, APIMANIFEST];

export function isClientType(type: string): boolean {
  return clientTypes.includes(type);
}

export function isPluginType(type: string): boolean {
  return pluginTypes.includes(type);
}

export async function updateTreeViewIcons(treeViewId: string, showIcons: boolean, showRegenerateIcon: boolean = false) {
    await vscode.commands.executeCommand('setContext', `${treeViewId}.showIcons`, showIcons);
    await vscode.commands.executeCommand('setContext', `${treeViewId}.showRegenerateIcon`, showRegenerateIcon);
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

export async function handleMigration(
  context: vscode.ExtensionContext,
  workspaceFolder: vscode.WorkspaceFolder
): Promise<void> {
  vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      title: vscode.l10n.t("Migrating your API clients..."),
      cancellable: false
  }, async (progress) => {
      progress.report({ increment: 0 });

      try {
          const migrationResult = await migrateFromLockFile(context, workspaceFolder.uri.fsPath);

          progress.report({ increment: 100 });

          if (migrationResult && migrationResult.length > 0) {
              displayMigrationMessages(migrationResult);
          } else {
              vscode.window.showWarningMessage(vscode.l10n.t("Migration completed, but no changes were detected."));
          }
      } catch (error) {
          vscode.window.showErrorMessage(vscode.l10n.t(`Migration failed: ${error}`));
      }
  });
}