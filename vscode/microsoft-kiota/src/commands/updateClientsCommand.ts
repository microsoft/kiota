import * as vscode from 'vscode';

import { API_MANIFEST_FILE, extensionId } from "../constants";
import { getExtensionSettings } from '../extensionSettings';
import { updateClients } from '../updateClients';
import { exportLogsAndShowErrors } from '../utilities/logging';
import { showUpgradeWarningMessage } from '../utilities/messaging';
import { updateStatusBarItem } from '../utilities/status';
import { Command } from "./Command";

interface UpdateClientsCommandProps {
  kiotaOutputChannel: vscode.LogOutputChannel;
  kiotaStatusBarItem: vscode.StatusBarItem;
}

export class UpdateClientsCommand extends Command {

  constructor(private context: vscode.ExtensionContext) {
    super();
  }

  public getName(): string {
    return `${extensionId}.updateClients`;
  }

  public async execute({ kiotaOutputChannel, kiotaStatusBarItem }: UpdateClientsCommandProps): Promise<void> {
    if (
      !vscode.workspace.workspaceFolders ||
      vscode.workspace.workspaceFolders.length === 0
    ) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("No workspace folder found, open a folder first")
      );
      return;
    }
    const existingApiManifestFileUris = await vscode.workspace.findFiles(`**/${API_MANIFEST_FILE}`);
    if (existingApiManifestFileUris.length > 0) {
      await Promise.all(existingApiManifestFileUris.map(uri => showUpgradeWarningMessage(uri, null, null, this.context)));
    }
    await updateStatusBarItem(this.context, kiotaOutputChannel, kiotaStatusBarItem);
    try {
      kiotaOutputChannel.clear();
      kiotaOutputChannel.show();
      kiotaOutputChannel.info(
        vscode.l10n.t("updating client with path {path}", {
          path: vscode.workspace.workspaceFolders[0].uri.fsPath,
        })
      );
      const res = await vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        cancellable: false,
        title: vscode.l10n.t("Updating clients...")
      }, (progress, _) => {
        const settings = getExtensionSettings(extensionId);
        return updateClients(this.context, settings.cleanOutput, settings.clearCache);
      });
      if (res) {
        await exportLogsAndShowErrors(res);
      }
    } catch (error) {
      kiotaOutputChannel.error(
        vscode.l10n.t("error updating the clients {error}"),
        error
      );
      await vscode.window.showErrorMessage(
        vscode.l10n.t("error updating the clients {error}"),
        error as string
      );
    }
  }

}