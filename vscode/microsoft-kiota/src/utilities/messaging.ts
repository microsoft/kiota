import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";

import { KIOTA_WORKSPACE_FILE } from "../constants";

export async function showUpgradeWarningMessage(clientPath: string, context: vscode.ExtensionContext): Promise<void> {
  const kiotaVersion = context.extension.packageJSON.kiotaVersion.toLocaleLowerCase();
  const workspaceFilePath = path.join(clientPath, KIOTA_WORKSPACE_FILE);
  if (!fs.existsSync(workspaceFilePath)) {
    return;
  }
  const workspaceFileData = await vscode.workspace.fs.readFile(vscode.Uri.file(workspaceFilePath));
  const workspaceFile = JSON.parse(workspaceFileData.toString()) as { kiotaVersion: string };
  // don't fail if kiotaVersion isn't in the workspace config file
  if (workspaceFile.kiotaVersion) {
    const clientVersion = workspaceFile.kiotaVersion.toLocaleLowerCase();
    if (clientVersion !== kiotaVersion) {
      await vscode.window.showWarningMessage(vscode.l10n.t("Client will be upgraded from version {0} to {1}, upgrade your dependencies", clientVersion, kiotaVersion));
    }
  }
}