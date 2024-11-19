import * as vscode from "vscode";

import { extensionId, treeViewFocusCommand } from "../constants";
import { WorkspaceTreeItem } from "../providers/workspaceTreeProvider";
import { Command } from "./Command";
import { ConsumerOperation, generationLanguageToString, getLogEntriesForLevel, KiotaLogEntry, LogLevel } from "../kiotaInterop";
import { getWorkspaceJsonDirectory, isPluginType, parseGenerationLanguage, parsePluginType } from "../util";
import { generateClient } from "./generate/generateClient";
import TelemetryReporter from "@vscode/extension-telemetry";
import { getDeepLinkParams } from "../handlers/deepLinkParamsHandler";
import { GenerateState } from "../modules/steps/generateSteps";
import { KiotaPluginType, KiotaGenerationLanguage } from "../types/enums";
import { ExtensionSettings } from "../types/extensionSettings";
import { exportLogsAndShowErrors } from "../utilities/logging";
import { generatePlugin } from "./generate/generatePlugin";
import { checkForSuccess } from "./generate/generation-util";
import { getLanguageInformationForDescription } from "./generate/getLanguageInformation";

export class DeleteWorkspaceItemCommand extends Command {
  constructor(private _context: vscode.ExtensionContext) {
    super();
  }

  public getName(): string {
    return `${extensionId}.workspace.deleteItem`;
  }

  public async execute(workspaceTreeItem: WorkspaceTreeItem): Promise<void> {
    const result = await this.deleteItem(isPluginType(workspaceTreeItem.category!) ? "plugin" : "client", workspaceTreeItem);
    vscode.window.showInformationMessage(`Delete item: ${workspaceTreeItem.label}`);
  }

  private async generatePluginAndRefreshUI(config: Partial<GenerateState>, outputPath: string, descriptionLocation: string): Promise<KiotaLogEntry[] | undefined> {
    const pluginTypes = Array.isArray(config.pluginTypes) ? parsePluginType(config.pluginTypes) : [KiotaPluginType.ApiPlugin];
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Generating plugin...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        this._context,
        descriptionLocation,
        outputPath,
        pluginTypes,
        [],
        [],
        typeof config.pluginName === "string"
          ? config.pluginName
          : "ApiClient",
        true,
        true,
        [],
        ConsumerOperation.Remove,
        config.workingDirectory
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
      reporter.sendRawTelemetryEvent(`${extensionId}.generatePlugin.completed`, {
        "pluginType": pluginTypes.toString(),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      return result;
    });
    if (result) {
      const isSuccess = await checkForSuccess(result);
      if (!isSuccess) {
        await exportLogsAndShowErrors(result);
      }
      const deepLinkParams = getDeepLinkParams();
      const isttkIntegration = deepLinkParams.source?.toLowerCase() === 'ttk';
      if (!isttkIntegration) {
        void vscode.window.showInformationMessage(vscode.l10n.t('Plugin generated successfully.'));
      }
    }
    return result;
  }
  private async generateClientAndRefreshUI(config: Partial<GenerateState>, outputPath: string, descriptionLocation: string): Promise<KiotaLogEntry[] | undefined> {
    const language =
      typeof config.language === "string"
        ? parseGenerationLanguage(config.language)
        : KiotaGenerationLanguage.CSharp;
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Generating client...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generateClient(
        this._context,
        descriptionLocation,
        outputPath,
        language,
        [],
        [],
        typeof config.clientClassName === "string"
          ? config.clientClassName
          : "ApiClient",
        typeof config.clientNamespaceName === "string"
          ? config.clientNamespaceName
          : "ApiSdk",
        true,
        true,
        true,
        true,
        [],
        [],
        [],
        [],
        true,
        ConsumerOperation.Remove,
        config.workingDirectory
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
      reporter.sendRawTelemetryEvent(`${extensionId}.generateClient.completed`, {
        "language": generationLanguageToString(language),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      return result;
    });

    if (result) {
      const isSuccess = await checkForSuccess(result);
      if (!isSuccess) {
        await exportLogsAndShowErrors(result);
      }
      void vscode.window.showInformationMessage(vscode.l10n.t('Generation completed successfully.'));
    }
    return result;
  }

  private async deleteItem(type: string, workspaceTreeItem: WorkspaceTreeItem): Promise<KiotaLogEntry[] | undefined> {
    const outputPath = workspaceTreeItem.properties?.outputPath!;
    const descriptionUrl = workspaceTreeItem.properties?.descriptionLocation!;
    const config: Partial<GenerateState> = {
      pluginTypes: [workspaceTreeItem.properties?.types!],
      workingDirectory: getWorkspaceJsonDirectory(), 
    };
    if (type === "plugin") {
      return await this.generatePluginAndRefreshUI(config, outputPath, descriptionUrl);
    } else {
      return await this.generateClientAndRefreshUI(config, outputPath, descriptionUrl);
    }
  }
}