import { ConsumerOperation, generateClient, generatePlugin, generationLanguageToString, getLanguageInformationForDescription, getLogEntriesForLevel, KiotaGenerationLanguage, KiotaPluginType, KiotaResult, LogLevel } from "@microsoft/kiota";
import { TelemetryReporter } from "@vscode/extension-telemetry";
import * as path from "path";
import * as vscode from "vscode";

import { API_MANIFEST_FILE, extensionId, treeViewFocusCommand, treeViewId } from "../../constants";
import { setGenerationConfiguration } from "../../handlers/configurationHandler";
import { clearDeepLinkParams, getDeepLinkParams } from "../../handlers/deepLinkParamsHandler";
import { GenerateState, generateSteps } from "../../modules/steps/generateSteps";
import { DependenciesViewProvider } from "../../providers/dependenciesViewProvider";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { GenerationType } from "../../types/enums";
import { ExtensionSettings, getExtensionSettings } from "../../types/extensionSettings";
import { GeneratedOutputState } from "../../types/GeneratedOutputState";
import { WorkspaceGenerationContext } from "../../types/WorkspaceGenerationContext";
import { getSanitizedString, getWorkspaceJsonDirectory, parseGenerationLanguage, parseGenerationType, parsePluginType, updateTreeViewIcons } from "../../util";
import { isDeeplinkEnabled, transformToGenerationConfig } from "../../utilities/deep-linking";
import { confirmDeletionOnCleanOutput } from "../../utilities/generation";
import { exportLogsAndShowErrors, logFromLogLevel } from "../../utilities/logging";
import { showUpgradeWarningMessage } from "../../utilities/messaging";
import { Command } from "../Command";
import { displayGenerationResults, getLanguageInformation } from "./generation-util";


export class GenerateClientCommand extends Command {

  constructor(
    private _openApiTreeProvider: OpenApiTreeProvider,
    private _context: vscode.ExtensionContext,
    private _dependenciesViewProvider: DependenciesViewProvider,
    private _setWorkspaceGenerationContext: (params: Partial<WorkspaceGenerationContext>) => void,
    private _kiotaOutputChannel: vscode.LogOutputChannel
  ) {
    super();
  }

  public getName(): string {
    return `${treeViewId}.generateClient`;
  }

  public async execute(): Promise<void> {
    const deepLinkParams = getDeepLinkParams();
    const selectedPaths = this._openApiTreeProvider.getSelectedPaths();
    if (selectedPaths.length === 0) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("No endpoints selected, select endpoints first")
      );
      return;
    }

    let languagesInformation = await getLanguageInformation();
    let availableStateInfo: Partial<GenerateState>;
    if (isDeeplinkEnabled(deepLinkParams)) {
      if (!deepLinkParams.name && this._openApiTreeProvider.apiTitle) {
        deepLinkParams.name = getSanitizedString(this._openApiTreeProvider.apiTitle);
      }
      availableStateInfo = await transformToGenerationConfig(deepLinkParams);
    } else {
      const pluginName = getSanitizedString(this._openApiTreeProvider.apiTitle);
      availableStateInfo = {
        clientClassName: this._openApiTreeProvider.clientClassName,
        clientNamespaceName: this._openApiTreeProvider.clientNamespaceName,
        language: this._openApiTreeProvider.language,
        outputPath: this._openApiTreeProvider.outputPath,
        pluginName
      };
    }
    const settings = getExtensionSettings(extensionId);
    if (settings.cleanOutput && !(await confirmDeletionOnCleanOutput())) {
      // cancel generation and open settings
      return vscode.commands.executeCommand('workbench.action.openSettings', 'kiota.cleanOutput.enabled');
    }
    let config = await generateSteps(
      availableStateInfo,
      languagesInformation,
      deepLinkParams
    );
    setGenerationConfiguration(config);
    const generationType = parseGenerationType(config.generationType);

    let outputPath = "./output";
    if (typeof config.outputPath === "string") {
      if (deepLinkParams.source?.toLowerCase() === 'ttk') {
        outputPath = path.join(config.outputPath, "appPackage");
      } else {
        outputPath = config.outputPath;
      }
    }

    let manifestKey = null;
    switch (config.generationType) {
      case "client":
        manifestKey = config.clientClassName;
        break;
      case "plugin":
      case "other":
        manifestKey = config.pluginName;
        break;
    }
    await showUpgradeWarningMessage(path.join(outputPath, ".kiota", API_MANIFEST_FILE), manifestKey, config.generationType as string);
    if (!this._openApiTreeProvider.descriptionUrl) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("No description found, select a description first")
      );
      return;
    }

    this._setWorkspaceGenerationContext({ generationType: config.generationType as string });
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

    const authenticationWarnings = getLogEntriesForLevel(result?.logs ?? [], LogLevel.warning).filter(entry => entry.message.startsWith('Authentication warning'));
    if (authenticationWarnings.length > 0) {
      authenticationWarnings.forEach(entry => logFromLogLevel(entry, this._kiotaOutputChannel));

      const showLogs = vscode.l10n.t("Show logs");
      const response = await vscode.window.showWarningMessage(
        vscode.l10n.t(
          "Incompatible security schemes for Copilot usage detected in the selected endpoints."),
        showLogs,
        vscode.l10n.t("Cancel")
      );
      if (response === showLogs) {
        this._kiotaOutputChannel.show();
      }
    }

    if (result?.isSuccess) {
      // Save state before opening the new window
      const outputState = {
        outputPath,
        config,
        clientClassName: config.clientClassName || config.pluginName
      };
      void this._context.workspaceState.update('generatedOutput', outputState as GeneratedOutputState);

      const pathOfSpec = path.join(outputPath, `${outputState.clientClassName?.toLowerCase()}-openapi.yml`);
      const pathPluginManifest = path.join(outputPath, `${outputState.clientClassName?.toLowerCase()}-apiplugin.json`);
      if (deepLinkParams.source?.toLowerCase() === 'ttk') {
        try {
          await vscode.commands.executeCommand(
            'fx-extension.createprojectfromkiota',
            [
              pathOfSpec,
              pathPluginManifest,
              deepLinkParams.ttkContext ? deepLinkParams.ttkContext : undefined
            ]
          );
          this._openApiTreeProvider.closeDescription();
          await updateTreeViewIcons(treeViewId, false);
        } catch (error) {
          const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
          reporter.sendTelemetryEvent("DeepLinked fx-extension.createprojectfromkiota", {
            "error": JSON.stringify(error)
          });
        }
      } else {
        if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
          await vscode.commands.executeCommand('vscode.openFolder', vscode.Uri.file(config.workingDirectory ?? getWorkspaceJsonDirectory()), true);
        } else {
          await displayGenerationResults(this._openApiTreeProvider, config);
        }
        await vscode.commands.executeCommand('kiota.workspace.refresh');
      }

      clearDeepLinkParams();  // Clear the state after successful generation
    }
  }

  private async generateManifestAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]): Promise<KiotaResult | undefined> {
    const pluginType = KiotaPluginType.ApiManifest;
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Generating manifest...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        {
          descriptionPath: this._openApiTreeProvider.descriptionUrl,
          outputPath: outputPath,
          pluginType,
          includePatterns: selectedPaths,
          excludePatterns: [],
          pluginName: typeof config.pluginName === "string"
            ? config.pluginName
            : "ApiClient",
          clearCache: settings.clearCache,
          cleanOutput: settings.cleanOutput,
          disabledValidationRules: settings.disableValidationRules,
          operation: ConsumerOperation.Add,
          pluginAuthType: null,
          pluginAuthRefid: '',
          workingDirectory: config.workingDirectory || getWorkspaceJsonDirectory()
        });
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result.logs, LogLevel.critical, LogLevel.error).length : 0;
      const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
      reporter.sendRawTelemetryEvent(`${extensionId}.generateManifest.completed`, {
        "pluginType": pluginType.toString(),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      return result;
    });
    if (result) {
      if (!result.isSuccess) {
        await exportLogsAndShowErrors(result.logs, this._kiotaOutputChannel);
      }
      void vscode.window.showInformationMessage(vscode.l10n.t('Generation completed successfully.'));
    }
    return result;
  }
  private async generatePluginAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]): Promise<KiotaResult | undefined> {
    const pluginTypes = Array.isArray(config.pluginTypes) ? parsePluginType(config.pluginTypes) : [KiotaPluginType.ApiPlugin];
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Generating plugin...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        {
          descriptionPath: this._openApiTreeProvider.descriptionUrl,
          outputPath: outputPath,
          pluginType: pluginTypes[0],
          includePatterns: selectedPaths,
          excludePatterns: [],
          pluginName: typeof config.pluginName === "string"
            ? config.pluginName
            : "ApiClient",
          clearCache: settings.clearCache,
          cleanOutput: settings.cleanOutput,
          disabledValidationRules: settings.disableValidationRules,
          operation: ConsumerOperation.Add,
          pluginAuthType: null,
          pluginAuthRefid: '',
          workingDirectory: config.workingDirectory || getWorkspaceJsonDirectory()
        });
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result.logs, LogLevel.critical, LogLevel.error).length : 0;
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
      if (!result.isSuccess) {
        await exportLogsAndShowErrors(result.logs, this._kiotaOutputChannel);
      }
      const deepLinkParams = getDeepLinkParams();
      const isttkIntegration = deepLinkParams.source?.toLowerCase() === 'ttk';
      if (!isttkIntegration) {
        void vscode.window.showInformationMessage(vscode.l10n.t('Plugin generated successfully.'));
      }
    }
    return result;
  }
  private async generateClientAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]): Promise<KiotaResult | undefined> {
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
        {
          openAPIFilePath: this._openApiTreeProvider.descriptionUrl,
          outputPath: outputPath, language,
          includePatterns: selectedPaths, excludePatterns: [],
          clientClassName: typeof config.clientClassName === "string"
            ? config.clientClassName
            : "ApiClient", clientNamespaceName: typeof config.clientNamespaceName === "string"
              ? config.clientNamespaceName
              : "ApiSdk",
          usesBackingStore: settings.backingStore,
          clearCache: settings.clearCache,
          cleanOutput: settings.cleanOutput,
          excludeBackwardCompatible: settings.excludeBackwardCompatible,
          disabledValidationRules: settings.disableValidationRules,
          serializers: settings.languagesSerializationConfiguration[language].serializers,
          deserializers: settings.languagesSerializationConfiguration[language].deserializers,
          structuredMimeTypes: settings.structuredMimeTypes,
          includeAdditionalData: settings.includeAdditionalData,
          operation: ConsumerOperation.Add,
          workingDirectory: config.workingDirectory ? config.workingDirectory : getWorkspaceJsonDirectory()
        });
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result.logs, LogLevel.critical, LogLevel.error).length : 0;
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
      {
        descriptionUrl: this._openApiTreeProvider.descriptionUrl,
        clearCache: settings.clearCache
      });
    if (languagesInformation) {
      this._dependenciesViewProvider.update(languagesInformation, language);
      await vscode.commands.executeCommand(treeViewFocusCommand);
    }
    if (result) {
      if (!result.isSuccess) {
        await exportLogsAndShowErrors(result.logs, this._kiotaOutputChannel);
      }
      void vscode.window.showInformationMessage(vscode.l10n.t('Generation completed successfully.'));
    }
    return result;
  }

}
