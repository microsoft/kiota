import * as vscode from "vscode";
import { ExtensionContext } from "vscode";
import TelemetryReporter from "@vscode/extension-telemetry";

import { extensionId, treeViewFocusCommand, treeViewId } from "../../constants";
import { ExtensionSettings, getExtensionSettings } from "../../extensionSettings";
import { generateClient } from '../../generateClient';
import { generatePlugin } from '../../generatePlugin';
import { getLanguageInformation, getLanguageInformationForDescription } from "../../getLanguageInformation";
import {
  ConsumerOperation,
  generationLanguageToString,
  getLogEntriesForLevel, KiotaGenerationLanguage, KiotaLogEntry, KiotaPluginType,
  LogLevel, parseGenerationLanguage, parsePluginType
} from "../../kiotaInterop";
import { DependenciesViewProvider } from "../../providers/dependenciesViewProvider";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { GenerateState, generateSteps, GenerationType, parseGenerationType } from "../../steps";
import { getWorkspaceJsonDirectory } from "../../util";
import { exportLogsAndShowErrors } from '../../utilities/logging';
import { showUpgradeWarningMessage } from "../../utilities/messaging";
import { Command } from "../Command";
import { GeneratedOutputState } from '../GeneratedOutputState';
import { displayGenerationResults } from "./generation-results";

export class GenerateClientCommand extends Command {

  private _context: ExtensionContext;
  private _openApiTreeProvider: OpenApiTreeProvider;

  public constructor(context: ExtensionContext, openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._context = context;
    this._openApiTreeProvider = openApiTreeProvider;
  }

  public toString(): string {
    return `${treeViewId}.generateClient`;
  }

  public async execute() {
    const selectedPaths = this._openApiTreeProvider.getSelectedPaths();
    if (selectedPaths.length === 0) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("No endpoints selected, select endpoints first")
      );
      return;
    }

    let languagesInformation = await getLanguageInformation(this._context);
    let config: Partial<GenerateState>;
    config = await generateSteps(
      {
        clientClassName: this._openApiTreeProvider.clientClassName,
        clientNamespaceName: this._openApiTreeProvider.clientNamespaceName,
        language: this._openApiTreeProvider.language,
        outputPath: this._openApiTreeProvider.outputPath,
      },
      languagesInformation
    );
    const generationType = parseGenerationType(config.generationType);
    const outputPath = typeof config.outputPath === "string"
      ? config.outputPath
      : "./output";
    await showUpgradeWarningMessage(this._context, outputPath);
    if (!this._openApiTreeProvider.descriptionUrl) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("No description found, select a description first")
      );
      return;
    }

    const settings = getExtensionSettings(extensionId);
    let result;
    switch (generationType) {
      case GenerationType.Client:
        result = await this.generateClientAndRefreshUI(config, settings, outputPath, selectedPaths);
        break;
      case GenerationType.Plugin:
        result = await this.generatePluginAndRefreshUI(config, settings, outputPath, selectedPaths);
        break;
      case GenerationType.ApiManifest:
        result = await this.generateManifestAndRefreshUI(config, settings, outputPath, selectedPaths);
        break;
      default:
        await vscode.window.showErrorMessage(
          vscode.l10n.t("Invalid generation type")
        );
        return;
    }
    if (result && getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length === 0) {
      // Save state before opening the new window
      void this._context.workspaceState.update('generatedOutput', {
        outputPath,
        config,
        clientClassName: config.clientClassName || config.pluginName
      } as GeneratedOutputState);
      if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
        await vscode.commands.executeCommand('vscode.openFolder', vscode.Uri.file(config.workingDirectory ?? getWorkspaceJsonDirectory()), true);
      } else {
        await displayGenerationResults(config, outputPath, this._openApiTreeProvider);
      }
    }
  }

  private async generateClientAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]): Promise<KiotaLogEntry[] | undefined> {
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
        this._openApiTreeProvider.descriptionUrl,
        outputPath,
        language,
        selectedPaths,
        [],
        typeof config.clientClassName === "string"
          ? config.clientClassName
          : "ApiClient",
        typeof config.clientNamespaceName === "string"
          ? config.clientNamespaceName
          : "ApiSdk",
        settings.backingStore,
        settings.clearCache,
        settings.cleanOutput,
        settings.excludeBackwardCompatible,
        settings.disableValidationRules,
        settings.languagesSerializationConfiguration[language].serializers,
        settings.languagesSerializationConfiguration[language].deserializers,
        settings.structuredMimeTypes,
        settings.includeAdditionalData,
        ConsumerOperation.Add,
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

    let languagesInformation = await getLanguageInformationForDescription(
      this._context,
      this._openApiTreeProvider.descriptionUrl,
      settings.clearCache
    );
    if (languagesInformation) {
      const dependenciesInfoProvider = new DependenciesViewProvider(
        this._context.extensionUri
      );
      dependenciesInfoProvider.update(languagesInformation, language);
      await vscode.commands.executeCommand(treeViewFocusCommand);
    }
    if (result) {
      await exportLogsAndShowErrors(result);
    }
    return result;
  }

  private async generatePluginAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]): Promise<KiotaLogEntry[] | undefined> {
    const pluginTypes = Array.isArray(config.pluginTypes) ? parsePluginType(config.pluginTypes) : [KiotaPluginType.ApiPlugin];
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Generating plugin...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        this._context,
        this._openApiTreeProvider.descriptionUrl,
        outputPath,
        pluginTypes,
        selectedPaths,
        [],
        typeof config.pluginName === "string"
          ? config.pluginName
          : "ApiClient",
        settings.clearCache,
        settings.cleanOutput,
        settings.disableValidationRules,
        ConsumerOperation.Add,
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
      await exportLogsAndShowErrors(result);
    }
    return result;
  }

  private async generateManifestAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]): Promise<KiotaLogEntry[] | undefined> {
    const pluginTypes = KiotaPluginType.ApiManifest;
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Generating manifest...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        this._context,
        this._openApiTreeProvider.descriptionUrl,
        outputPath,
        [pluginTypes],
        selectedPaths,
        [],
        typeof config.pluginName === "string"
          ? config.pluginName
          : "ApiClient",
        settings.clearCache,
        settings.cleanOutput,
        settings.disableValidationRules,
        ConsumerOperation.Add,
        config.workingDirectory
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
    
      reporter.sendRawTelemetryEvent(`${extensionId}.generateManifest.completed`, {
        "pluginType": pluginTypes.toString(),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      return result;
    });
    if (result) {
      await exportLogsAndShowErrors(result);
    }
    return result;
  }
}