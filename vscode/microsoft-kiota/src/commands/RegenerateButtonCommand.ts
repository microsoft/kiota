import * as vscode from "vscode";
import { ExtensionContext } from "vscode";

import { extensionId } from "../constants";
import { ExtensionSettings, getExtensionSettings } from "../extensionSettings";
import { generateClient } from "../generateClient";
import { generatePlugin } from "../generatePlugin";
import {
  ClientObjectProperties,
  ClientOrPluginProperties, ConsumerOperation, getLogEntriesForLevel,
  KiotaGenerationLanguage, KiotaPluginType, parseGenerationLanguage, parsePluginType,
  PluginObjectProperties
} from "../kiotaInterop";
import { OpenApiTreeProvider } from "../openApiTreeProvider";
import { GenerateState } from "../steps";
import { isClientType, isPluginType } from "../util";
import { Command } from "./Command";

export class RegenerateButtonCommand extends Command {

  private _context: ExtensionContext;
  private _openApiTreeProvider: OpenApiTreeProvider;
  private _clientKey: string;
  private _clientObject: ClientOrPluginProperties;
  private _workspaceGenerationType: string;

  public constructor(context: ExtensionContext, openApiTreeProvider: OpenApiTreeProvider,
    clientKey: string, clientObject: ClientOrPluginProperties, workspaceGenerationType: string) {
    super();
    this._context = context;
    this._openApiTreeProvider = openApiTreeProvider;
    this._clientKey = clientKey;
    this._clientObject = clientObject;
    this._workspaceGenerationType = workspaceGenerationType;
  }

  async execute(config: Partial<GenerateState>): Promise<void> {
    if (!this._clientKey || this._clientKey === '') {
      this._clientKey = config.clientClassName || config.pluginName || '';
    }
    if (!config) {
      config = {
        outputPath: this._clientObject.outputPath,
        clientClassName: this._clientKey,
      };
    }
    const settings = getExtensionSettings(extensionId);
    const selectedPaths = this._openApiTreeProvider.getSelectedPaths();
    if (selectedPaths.length === 0) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("No endpoints selected, select endpoints first")
      );
      return;
    }
    if (isClientType(this._workspaceGenerationType)) {
      await this.regenerateClient(settings, selectedPaths);
    }
    if (isPluginType(this._workspaceGenerationType)) {
      await this.regeneratePlugin(settings, selectedPaths);
    }
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
        this._clientObject.descriptionLocation ? this._clientObject.descriptionLocation : this._openApiTreeProvider.descriptionUrl,
        this._clientObject.outputPath,
        language,
        selectedPaths ? selectedPaths : this._clientObject.includePatterns,
        this._clientObject.excludePatterns ? this._clientObject.excludePatterns : [],
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
      return result;
    });
    void vscode.window.showInformationMessage(`Client ${this._clientKey} re-generated successfully.`);
    this._openApiTreeProvider.resetInitialState();
  }
  async regeneratePlugin(settings: ExtensionSettings, selectedPaths?: string[]) {
    const clientObjectItem = this._clientObject as PluginObjectProperties;

    const pluginTypes = Array.isArray(clientObjectItem.types) ? parsePluginType(clientObjectItem.types) : [KiotaPluginType.ApiPlugin];
    await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Re-generating plugin...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        this._context,
        this._clientObject.descriptionLocation ? this._clientObject.descriptionLocation : this._openApiTreeProvider.descriptionUrl,
        this._clientObject.outputPath,
        pluginTypes,
        selectedPaths ? selectedPaths : this._clientObject.includePatterns,
        [],
        this._clientKey,
        settings.clearCache,
        settings.cleanOutput,
        settings.disableValidationRules,
        ConsumerOperation.Edit
      );

      // TODO: uncomment when telemetry is implemented

      /* const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, vscode.LogLevel.critical, vscode.LogLevel.error).length : 0;
      reporter.sendRawTelemetryEvent(`${extensionId}.re-generatePlugin.completed`, {
        "pluginType": pluginTypes.toString(),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      }); */
      return result;
    });
    void vscode.window.showInformationMessage(`Plugin ${this._clientKey} re-generated successfully.`);
    this._openApiTreeProvider.resetInitialState();
  }
}
