import TelemetryReporter from "@vscode/extension-telemetry";
import * as vscode from "vscode";

import { extensionId } from "../../constants";
import { getLogEntriesForLevel, KiotaResult, LogLevel, removeClient, removePlugin } from "../../kiotaInterop";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { SharedService } from "../../providers/sharedService";
import { WorkspaceTreeItem } from "../../providers/workspaceTreeProvider";
import { getWorkspaceJsonDirectory, isPluginType } from "../../util";
import { exportLogsAndShowErrors } from "../../utilities/logging";
import { Command } from "../Command";

export class DeleteWorkspaceItemCommand extends Command {
  constructor(
    private _context: vscode.ExtensionContext,
    private _openApiTreeProvider: OpenApiTreeProvider,
    private _kiotaOutputChannel: vscode.LogOutputChannel,
    private sharedService: SharedService
  ) {
    super();
  }

  public getName(): string {
    return `${extensionId}.workspace.deleteItem`;
  }

  public async execute(workspaceTreeItem: WorkspaceTreeItem): Promise<void> {
    const type = workspaceTreeItem.category && isPluginType(workspaceTreeItem.category) ? "plugin" : "client";
    const yesAnswer: vscode.MessageItem = { title: vscode.l10n.t("Yes") };
    const noAnswer: vscode.MessageItem = { title: vscode.l10n.t("No") };

    const response = await vscode.window.showWarningMessage(
      vscode.l10n.t("Do you want to delete this item?"),
      yesAnswer,
      noAnswer
    );

    if (response?.title === yesAnswer.title) {
      if (this.sharedService.get('clientOrPluginKey') === workspaceTreeItem.label) {
        this._openApiTreeProvider.closeDescription();
      }

      const result = await this.deleteItem(type, workspaceTreeItem);
      if (result) {
        await vscode.commands.executeCommand('kiota.workspace.refresh');
        if (result.isSuccess) {
          void vscode.window.showInformationMessage(vscode.l10n.t('{0} removed successfully.', workspaceTreeItem.label));
        } else {
          await exportLogsAndShowErrors(result.logs, this._kiotaOutputChannel);
        }
      }
    }
  }

  private async deleteItem(type: string, workspaceTreeItem: WorkspaceTreeItem): Promise<KiotaResult | undefined> {
    const itemName = workspaceTreeItem.label;
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t(`Removing ${type}...`)
    }, async (progress, _) => {
      const start = performance.now();
      const result = type === "plugin" ? await removePlugin(
        { pluginName: itemName, cleanOutput: false, workingDirectory: getWorkspaceJsonDirectory() },
      ) : await removeClient(
        { clientName: itemName, cleanOutput: false, workingDirectory: getWorkspaceJsonDirectory() },
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result.logs, LogLevel.critical, LogLevel.error).length : 0;
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

