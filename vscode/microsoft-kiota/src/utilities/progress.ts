import * as vscode from "vscode";

import { treeViewFocusCommand } from "../constants";

function openTreeViewWithProgress<T>(callback: () => Promise<T>): Thenable<T> {
  return vscode.window.withProgress({
    location: vscode.ProgressLocation.Notification,
    cancellable: false,
    title: vscode.l10n.t("Loading...")
  }, async (progress, _) => {
    try {
      const result = await callback();
      await vscode.commands.executeCommand(treeViewFocusCommand);
      return result;
    } catch (error) {
      throw error;
    }
  });
}

export { openTreeViewWithProgress };
