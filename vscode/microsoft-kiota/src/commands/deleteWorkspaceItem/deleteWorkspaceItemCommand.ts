import TelemetryReporter from "@vscode/extension-telemetry";
import * as vscode from "vscode";

import { extensionId } from "../../constants";
import { getLogEntriesForLevel, KiotaLogEntry, LogLevel } from "../../kiotaInterop";
import { WorkspaceTreeItem } from "../../providers/workspaceTreeProvider";
import { isPluginType } from "../../util";
import { exportLogsAndShowErrors } from "../../utilities/logging";
import { Command } from "../Command";
import { removeClient, removePlugin } from "./removeItem";

export class DeleteWorkspaceItemCommand extends Command {
  constructor(
    private _context: vscode.ExtensionContext,
    private _kiotaOutputChannel: vscode.LogOutputChannel
  ) {
    super();
  }

  public getName(): string {
    return `${extensionId}.workspace.deleteItem`;
  }

  public async execute(workspaceTreeItem: WorkspaceTreeItem): Promise<void> {
    const result = await this.deleteItem(isPluginType(workspaceTreeItem.category!) ? "plugin" : "client", workspaceTreeItem);
    if (result) {
      const isSuccess = result.some(k => k.message.includes('removed successfully'));
      if (isSuccess) {
        void vscode.window.showInformationMessage(vscode.l10n.t('{0} removed successfully.', workspaceTreeItem.label));
        await vscode.commands.executeCommand('kiota.workspace.refresh');
      } else {
        await exportLogsAndShowErrors(result, this._kiotaOutputChannel);
      }
    }
  }

  private async deleteItem(type: string, workspaceTreeItem: WorkspaceTreeItem): Promise<KiotaLogEntry[] | undefined> {
    if (type === "plugin") {
      return await this.deletePlugin(workspaceTreeItem.label);
    } else {
      return await this.deleteClient(workspaceTreeItem.label);
    }
  }

  private async deletePlugin(pluginName: string): Promise<KiotaLogEntry[] | undefined> {
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Removing plugin...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await removePlugin(
        this._context,
        pluginName!,
        false,
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
      reporter.sendRawTelemetryEvent(`${extensionId}.removePlugin.completed`, {
        "pluginType": pluginName,
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      return result;
    });
    return result;
  }
  private async deleteClient(clientName: string): Promise<KiotaLogEntry[] | undefined> {
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Removing client...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await removeClient(
        this._context,
        clientName,
        false,
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
      reporter.sendRawTelemetryEvent(`${extensionId}.removeClient.completed`, {
        "client": clientName,
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      return result;
    });
    return result;
  }
}

