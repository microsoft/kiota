import * as fs from 'fs';
import * as path from 'path';
import * as vscode from "vscode";
import { ExtensionContext } from 'vscode';

import { KIOTA_WORKSPACE_FILE } from '../constants';

async function showUpgradeWarningMessage(context: ExtensionContext, clientPath: string): Promise<void> {
  const kiotaVersion = context.extension.packageJSON.kiotaVersion.toLocaleLowerCase();
  const lockFilePath = path.join(clientPath, KIOTA_WORKSPACE_FILE);
  if (!fs.existsSync(lockFilePath)) {
    return;
  }
  const lockFileData = await vscode.workspace.fs.readFile(vscode.Uri.file(lockFilePath));
  const lockFile = JSON.parse(lockFileData.toString()) as { kiotaVersion: string };
  const clientVersion = lockFile.kiotaVersion.toLocaleLowerCase();
  if (clientVersion.toLocaleLowerCase() !== kiotaVersion) {
    await vscode.window.showWarningMessage(vscode.l10n.t("Client will be upgraded from version {0} to {1}, upgrade your dependencies", clientVersion, kiotaVersion));
  }
}

export {
  showUpgradeWarningMessage
};