import * as path from 'path';
import * as vscode from "vscode";

import TelemetryReporter from "@vscode/extension-telemetry";
import { extensionId } from "../constants";
import { ClientObjectProperties, ConsumerOperation, generationLanguageToString, getLogEntriesForLevel, KiotaLogEntry, LogLevel, PluginObjectProperties } from "../kiotaInterop";
import { GenerateState } from "../modules/steps/generateSteps";
import { WorkspaceTreeItem, WorkspaceTreeProvider } from "../providers/workspaceTreeProvider";
import { KiotaGenerationLanguage, KiotaPluginType } from "../types/enums";
import { getWorkspaceJsonDirectory, isPluginType, parseGenerationLanguage, parsePluginType } from "../util";
import { exportLogsAndShowErrors } from "../utilities/logging";
import { Command } from "./Command";
import { generateClient } from "./generate/generateClient";
import { generatePlugin } from "./generate/generatePlugin";
import { checkForSuccess } from "./generate/generation-util";

export class DeleteWorkspaceItemCommand extends Command {
  constructor(private _context: vscode.ExtensionContext, private _workspaceTreeProvider: WorkspaceTreeProvider) {
    super();
  }

  public getName(): string {
    return `${extensionId}.workspace.deleteItem`;
  }

  public async execute(workspaceTreeItem: WorkspaceTreeItem): Promise<void> {
    const result = await this.deleteItem(isPluginType(workspaceTreeItem.category!) ? "plugin" : "client", workspaceTreeItem);
    if (result) {
      const isSuccess = await checkForSuccess(result);
      if (isSuccess) {
        await this._workspaceTreeProvider.refreshView();
        void vscode.window.showInformationMessage(vscode.l10n.t(`${workspaceTreeItem.label} removed successfully.`));
      } else {
        await exportLogsAndShowErrors(result);
      }
    }
  }

  private async deleteItem(type: string, workspaceTreeItem: WorkspaceTreeItem): Promise<KiotaLogEntry[] | undefined> {
    const outputPath = workspaceTreeItem.properties?.outputPath!;
    const descriptionUrl = workspaceTreeItem.properties?.descriptionLocation!;

    const config: Partial<GenerateState> = {
      workingDirectory: getWorkspaceJsonDirectory(),
    };
    const absoluteOutputPath = path.join(config.workingDirectory!, outputPath);

    if (type === "plugin") {
      const properties = workspaceTreeItem.properties as PluginObjectProperties;
      config.pluginTypes = properties.types;
      config.pluginName = workspaceTreeItem.label;
      return await this.removePlugin(config, absoluteOutputPath, descriptionUrl);
    } else {
      const properties = workspaceTreeItem.properties as ClientObjectProperties;
      config.clientClassName = workspaceTreeItem.label;
      config.clientNamespaceName = properties.clientNamespaceName;
      return await this.removeClient(config, absoluteOutputPath, descriptionUrl);
    }
  }

  private async removePlugin(config: Partial<GenerateState>, outputPath: string, descriptionLocation: string): Promise<KiotaLogEntry[] | undefined> {
    const pluginTypes = Array.isArray(config.pluginTypes) ? parsePluginType(config.pluginTypes) : [KiotaPluginType.ApiPlugin];
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Removing plugin...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        this._context,
        descriptionLocation,
        outputPath,
        pluginTypes,
        [],
        [],
        config.pluginName!,
        true,
        false,
        [],
        ConsumerOperation.Remove,
        config.workingDirectory
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
      reporter.sendRawTelemetryEvent(`${extensionId}.removePlugin.completed`, {
        "pluginType": pluginTypes.toString(),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      return result;
    });
    return result;
  }
  private async removeClient(config: Partial<GenerateState>, outputPath: string, descriptionLocation: string): Promise<KiotaLogEntry[] | undefined> {
    const language =
      typeof config.language === "string"
        ? parseGenerationLanguage(config.language)
        : KiotaGenerationLanguage.CSharp;
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Removing client...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generateClient(
        this._context,
        descriptionLocation,
        outputPath,
        language,
        [],
        [],
        config.clientClassName!,
        config.clientNamespaceName! as string,
        false,
        true,
        false,
        false,
        [],
        [],
        [],
        [],
        false,
        ConsumerOperation.Remove,
        config.workingDirectory
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
      reporter.sendRawTelemetryEvent(`${extensionId}.removeClient.completed`, {
        "language": generationLanguageToString(language),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      return result;
    });
    return result;
  }
}