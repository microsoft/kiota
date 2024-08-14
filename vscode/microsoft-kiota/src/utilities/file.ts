import * as vscode from "vscode";

import { treeViewFocusCommand, treeViewId } from "../constants";
import { OpenApiTreeProvider } from "../providers/openApiTreeProvider";
import { updateTreeViewIcons } from "../util";

async function loadLockFile(node: { fsPath: string }, openApiTreeProvider: OpenApiTreeProvider, clientOrPluginName?: string): Promise<void> {
  await openTreeViewWithProgress(() => openApiTreeProvider.loadLockFile(node.fsPath, clientOrPluginName));
  await updateTreeViewIcons(treeViewId, true);
}

function openTreeViewWithProgress<T>(callback: () => Promise<T>): Thenable<T> {
  return vscode.window.withProgress({
    location: vscode.ProgressLocation.Notification,
    cancellable: false,
    title: vscode.l10n.t("Loading...")
  }, async (progress, _) => {
    const result = await callback();
    await vscode.commands.executeCommand(treeViewFocusCommand);
    return result;
  });
}

export {
  loadLockFile,
  openTreeViewWithProgress
};

