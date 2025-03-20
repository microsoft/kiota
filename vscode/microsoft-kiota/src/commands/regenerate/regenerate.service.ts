import TelemetryReporter from "@vscode/extension-telemetry";
import * as path from "path";
import * as vscode from "vscode";
import { ExtensionContext } from "vscode";

import { extensionId } from "../../constants";
import { ClientObjectProperties, ClientOrPluginProperties, ConsumerOperation, generateClient, generatePlugin, getLogEntriesForLevel, KiotaGenerationLanguage, KiotaPluginType, LogLevel, PluginObjectProperties } from "../../kiotaInterop";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { ExtensionSettings } from "../../types/extensionSettings";
import { getWorkspaceJsonDirectory, parseGenerationLanguage, parsePluginType } from "../../util";
import { exportLogsAndShowErrors } from "../../utilities/logging";

export class RegenerateService {

  public constructor(private _context: ExtensionContext, private _openApiTreeProvider: OpenApiTreeProvider,
    private _clientKey: string, private _clientObject: ClientOrPluginProperties, private _kiotaOutputChannel: vscode.LogOutputChannel) {
  }

  async regenerateClient(settings: ExtensionSettings, selectedPaths?: string[]): Promise<void> {
    const clientObjectItem = this._clientObject as ClientObjectProperties;
    const language =
      typeof clientObjectItem.language === "string"
        ? parseGenerationLanguage(clientObjectItem.language)
        : KiotaGenerationLanguage.CSharp;
    await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Re-generating client...")
    }, async (progress, _) => {
      const result = await generateClient(
        {
          openAPIFilePath: clientObjectItem.descriptionLocation ? clientObjectItem.descriptionLocation : this._openApiTreeProvider.descriptionUrl,
          outputPath: clientObjectItem.outputPath,
          language,
          includePatterns: selectedPaths ? selectedPaths : clientObjectItem.includePatterns,
          excludePatterns: clientObjectItem.excludePatterns ? clientObjectItem.excludePatterns : [], clientClassName: this._clientKey,
          clientNamespaceName: clientObjectItem.clientNamespaceName,
          usesBackingStore: clientObjectItem.usesBackingStore ? clientObjectItem.usesBackingStore : settings.backingStore,
          clearCache: settings.clearCache,
          cleanOutput: settings.cleanOutput,
          excludeBackwardCompatible: clientObjectItem.excludeBackwardCompatible ? clientObjectItem.excludeBackwardCompatible : settings.excludeBackwardCompatible,
          disabledValidationRules: clientObjectItem.disabledValidationRules ? clientObjectItem.disabledValidationRules : settings.disableValidationRules,
          serializers: settings.languagesSerializationConfiguration[language].serializers,
          deserializers: settings.languagesSerializationConfiguration[language].deserializers,
          structuredMimeTypes: clientObjectItem.structuredMimeTypes ? clientObjectItem.structuredMimeTypes : settings.structuredMimeTypes,
          includeAdditionalData: clientObjectItem.includeAdditionalData ? clientObjectItem.includeAdditionalData : settings.includeAdditionalData,
          operation: ConsumerOperation.Edit,
          workingDirectory: getWorkspaceJsonDirectory()
        });
      if (result) {
        if (!result.isSuccess) {
          await exportLogsAndShowErrors(result.logs, this._kiotaOutputChannel);
        }
        void vscode.window.showInformationMessage(`Client ${this._clientKey} re-generated successfully.`);
      }
      return result;
    });

    this._openApiTreeProvider.resetInitialState();
  }

  async regeneratePlugin(settings: ExtensionSettings, selectedPaths?: string[]) {
    const pluginObjectItem = this._clientObject as PluginObjectProperties;
    const pluginTypes = Array.isArray(pluginObjectItem.types) ? parsePluginType(pluginObjectItem.types) : [KiotaPluginType.ApiPlugin];
    await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Re-generating plugin...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        {
          openAPIFilePath: pluginObjectItem.descriptionLocation ? pluginObjectItem.descriptionLocation : this._openApiTreeProvider.descriptionUrl,
          outputPath: pluginObjectItem.outputPath,
          pluginTypes,
          includePatterns: selectedPaths ? selectedPaths : pluginObjectItem.includePatterns,
          excludePatterns: [],
          clientClassName: this._clientKey,
          clearCache: settings.clearCache,
          cleanOutput: false,
          disabledValidationRules: settings.disableValidationRules,
          operation: ConsumerOperation.Edit,
          pluginAuthType: pluginObjectItem.authType ? pluginObjectItem.authType : null,
          pluginAuthRefid: pluginObjectItem.authReferenceId ? pluginObjectItem.authReferenceId : '',
          workingDirectory: getWorkspaceJsonDirectory()
        },

      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result.logs, LogLevel.critical, LogLevel.error).length : 0;

      const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
      reporter.sendRawTelemetryEvent(`${extensionId}.re-generatePlugin.completed`, {
        "pluginType": pluginTypes.toString(),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      if (result) {
        if (!result.isSuccess) {
          await exportLogsAndShowErrors(result.logs, this._kiotaOutputChannel);
        }
        void vscode.window.showInformationMessage(vscode.l10n.t(`Plugin ${this._clientKey} re-generated successfully.`));
      }
      return result;
    });
    this._openApiTreeProvider.resetInitialState();
  }

  async regenerateTeamsApp(workspaceJson: vscode.TextDocument, clientOrPluginKey: string) {
    const workspaceDirectory = path.dirname(workspaceJson.fileName);
    const workspaceParentDirectory = path.dirname(workspaceDirectory);
    const shouldMakeTTKFunctionCall = await this.followsTTKFolderStructure(workspaceParentDirectory);

    if (shouldMakeTTKFunctionCall) {
      const workspaceJsonContent = workspaceJson.getText();
      const workspaceJsonData = JSON.parse(workspaceJsonContent);

      const outputPath = workspaceJsonData.plugins[clientOrPluginKey].outputPath;
      const pathOfSpec = path.join(workspaceParentDirectory, outputPath, `${clientOrPluginKey.toLowerCase()}-openapi.yml`);
      const pathPluginManifest = path.join(workspaceParentDirectory, outputPath, `${clientOrPluginKey.toLowerCase()}-apiplugin.json`);

      await vscode.commands.executeCommand(
        'fx-extension.kiotaregenerate',
        [
          pathOfSpec,
          pathPluginManifest
        ]
      );
    }
  }

  private async followsTTKFolderStructure(workspaceParentDirectory: string): Promise<boolean> {
    const filesAndFolders = await vscode.workspace.fs.readDirectory(vscode.Uri.file(workspaceParentDirectory));
    return !!filesAndFolders.find(([name, type]) => type === vscode.FileType.File && name === 'teamsapp.yml');
  }
}