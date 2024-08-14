import * as path from 'path';
import * as vscode from "vscode";
import { ExtensionContext, StatusBarItem } from 'vscode';

import { KIOTA_WORKSPACE_FILE, extensionId } from "../constants";
import { getExtensionSettings } from "../extensionSettings";
import { updateClients } from "../updateClients";
import { exportLogsAndShowErrors, kiotaOutputChannel } from "../utilities/logging";
import { showUpgradeWarningMessage } from "../utilities/messaging";
import { updateStatusBarItem } from '../utilities/status-bar';
import { Command } from "./Command";

export class UpdateClientsCommand extends Command {

  private _context: ExtensionContext;

  public constructor(context: ExtensionContext) {
    super();
    this._context = context;
  }

  async execute(kiotaStatusBarItem: StatusBarItem): Promise<void> {
    if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("No workspace folder found, open a folder first")
      );
      return;
    }
    const existingLockFileUris = await vscode.workspace.findFiles(`**/${KIOTA_WORKSPACE_FILE}`);
    if (existingLockFileUris.length > 0) {
      await Promise.all(existingLockFileUris.map(x => path.dirname(x.fsPath)).map(x => showUpgradeWarningMessage(this._context, x)));
    }
    await updateStatusBarItem(this._context, kiotaStatusBarItem);
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
        return updateClients(this._context, settings.cleanOutput, settings.clearCache);
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