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
    return await this.removeWorkspaceItem(workspaceTreeItem.label, type);
  }

  private async removeWorkspaceItem(itemName: string, type: string): Promise<KiotaLogEntry[] | undefined> {
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t(`Removing ${type}...`)
    }, async (progress, _) => {
      const start = performance.now();
      const result = type === "plugin" ? await removePlugin(
        this._context,
        itemName,
        false,
      ) : await removeClient(
        this._context,
        itemName,
        false,
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
      reporter.sendRawTelemetryEvent(`${extensionId}.remove${type}.completed`, {
        "pluginType": itemName,
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      return result;
    });
    return result;
  }
}

