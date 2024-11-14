import * as vscode from 'vscode';

import { getKiotaVersion } from '../kiotaInterop/getKiotaVersion';

async function updateStatusBarItem(context: vscode.ExtensionContext, kiotaOutputChannel: vscode.LogOutputChannel, kiotaStatusBarItem: vscode.StatusBarItem): Promise<void> {
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