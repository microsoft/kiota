import * as vscode from "vscode";

import { ExtensionContext, StatusBarItem } from "vscode";
import { getKiotaVersion } from "./getKiotaVersion";
import { kiotaOutputChannel } from "./logging";

async function updateStatusBarItem(context: ExtensionContext, kiotaStatusBarItem: StatusBarItem): Promise<void> {
  try {
    const version = await getKiotaVersion(context, kiotaOutputChannel);
    if (!version) {
      throw new Error("kiota not found");
    }
    kiotaStatusBarItem.text = `$(extensions-info-message) kiota ${version}`;
  } catch (error) {
    kiotaStatusBarItem.text = `$(extensions-warning-message) kiota ${vscode.l10n.t(
      "not found"
    )}`;
    kiotaStatusBarItem.backgroundColor = new vscode.ThemeColor(
      "statusBarItem.errorBackground"
    );
  }
  kiotaStatusBarItem.show();
}

export { updateStatusBarItem };