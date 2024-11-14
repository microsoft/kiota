import TelemetryReporter from "@vscode/extension-telemetry";
import * as path from "path";
import * as vscode from "vscode";
import { ExtensionContext } from "vscode";

import { extensionId } from "../../constants";
import { ClientObjectProperties, ClientOrPluginProperties, ConsumerOperation, getLogEntriesForLevel, LogLevel, PluginObjectProperties } from "../../kiotaInterop";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { KiotaGenerationLanguage, KiotaPluginType } from "../../types/enums";
import { ExtensionSettings } from "../../types/extensionSettings";
import { parseGenerationLanguage, parsePluginType } from "../../util";
import { exportLogsAndShowErrors } from "../../utilities/logging";
import { generateClient } from "../generate/generateClient";
import { generatePlugin } from "../generate/generatePlugin";
import { checkForSuccess } from "../generate/generation-util";

export class RegenerateService {
  private _context: ExtensionContext;
  private _openApiTreeProvider: OpenApiTreeProvider;
  private _clientKey: string;
  private _clientObject: ClientOrPluginProperties;

  public constructor(context: ExtensionContext, openApiTreeProvider: OpenApiTreeProvider,
    clientKey: string, clientObject: ClientOrPluginProperties) {
    this._context = context;
    this._openApiTreeProvider = openApiTreeProvider;
    this._clientKey = clientKey;
    this._clientObject = clientObject;
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
        this._context,
        clientObjectItem.descriptionLocation ? clientObjectItem.descriptionLocation : this._openApiTreeProvider.descriptionUrl,
        clientObjectItem.outputPath,
        language,
        selectedPaths ? selectedPaths : clientObjectItem.includePatterns,
        clientObjectItem.excludePatterns ? clientObjectItem.excludePatterns : [],
        this._clientKey,
        clientObjectItem.clientNamespaceName,
        clientObjectItem.usesBackingStore ? clientObjectItem.usesBackingStore : settings.backingStore,
        true, // clearCache
        true, // cleanOutput
        clientObjectItem.excludeBackwardCompatible ? clientObjectItem.excludeBackwardCompatible : settings.excludeBackwardCompatible,
        clientObjectItem.disabledValidationRules ? clientObjectItem.disabledValidationRules : settings.disableValidationRules,
        settings.languagesSerializationConfiguration[language].serializers,
        settings.languagesSerializationConfiguration[language].deserializers,
        clientObjectItem.structuredMimeTypes ? clientObjectItem.structuredMimeTypes : settings.structuredMimeTypes,
        clientObjectItem.includeAdditionalData ? clientObjectItem.includeAdditionalData : settings.includeAdditionalData,
        ConsumerOperation.Edit
      );
      if (result) {
        const isSuccess = await checkForSuccess(result);
        if (!isSuccess) {
          await exportLogsAndShowErrors(result);
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
        this._context,
        pluginObjectItem.descriptionLocation ? pluginObjectItem.descriptionLocation : this._openApiTreeProvider.descriptionUrl,
        pluginObjectItem.outputPath,
        pluginTypes,
        selectedPaths ? selectedPaths : pluginObjectItem.includePatterns,
        [],
        this._clientKey,
        settings.clearCache,
        false,
        settings.disableValidationRules,
        ConsumerOperation.Edit
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;

      const reporter = new TelemetryReporter(this._context.extension.packageJSON.telemetryInstrumentationKey);
      reporter.sendRawTelemetryEvent(`${extensionId}.re-generatePlugin.completed`, {
        "pluginType": pluginTypes.toString(),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      if (result) {
        const isSuccess = await checkForSuccess(result);
        if (!isSuccess) {
          await exportLogsAndShowErrors(result);
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