import * as vscode from "vscode";
import { ExtensionContext } from "vscode";

import { extensionId } from "../../constants";
import { ExtensionSettings } from "../../extensionSettings";
import { generateClient } from "../../generateClient";
import { generatePlugin } from "../../generatePlugin";
import {
  ClientObjectProperties, ClientOrPluginProperties, ConsumerOperation, getLogEntriesForLevel,
  KiotaGenerationLanguage, KiotaPluginType, LogLevel, parseGenerationLanguage, parsePluginType,
  PluginObjectProperties
} from "../../kiotaInterop";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { Telemetry } from "../../telemetry";

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

      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      const reporter = Telemetry.reporter;
      reporter.sendRawTelemetryEvent(`${extensionId}.re-generatePlugin.completed`, {
        "pluginType": pluginTypes.toString(),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      return result;
    });
    void vscode.window.showInformationMessage(`Plugin ${this._clientKey} re-generated successfully.`);
    this._openApiTreeProvider.resetInitialState();
  }
}