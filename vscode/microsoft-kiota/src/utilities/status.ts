import * as vscode from 'vscode';

import { getKiotaVersion } from '../kiotaInterop';

async function updateStatusBarItem(kiotaOutputChannel: vscode.LogOutputChannel, kiotaStatusBarItem: vscode.StatusBarItem): Promise<void> {
  try {
    const version = await getKiotaVersion();
    if (!version) {
      throw new Error("kiota not found");
    }
    kiotaStatusBarItem.text = `$(extensions-info-message) kiota ${version}`;
    kiotaOutputChannel.info(`kiota: ${version}`);
  } catch (error) {
    kiotaStatusBarItem.text = `$(extensions-warning-message) kiota ${vscode.l10n.t(
      "not found"
    )}`;
    kiotaStatusBarItem.backgroundColor = new vscode.ThemeColor(
      "statusBarItem.errorBackground"
    );
    kiotaOutputChannel.error(`kiota: ${vscode.l10n.t('not found')}`);
    kiotaOutputChannel.show();
  }
  kiotaStatusBarItem.show();
}

export { updateStatusBarItem };