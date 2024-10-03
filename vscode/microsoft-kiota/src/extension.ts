// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import TelemetryReporter from '@vscode/extension-telemetry';
import * as fs from 'fs';
import * as path from 'path';
import * as vscode from "vscode";

import { CodeLensProvider } from "./codelensProvider";
import { EditPathsCommand } from './commands/editPathsCommand';
import { MigrateFromLockFileCommand } from './commands/migrateFromLockFileCommand';
import { AddAllToSelectedEndpointsCommand } from './commands/open-api-tree-view/addAllToSelectedEndpointsCommand';
import { AddToSelectedEndpointsCommand } from './commands/open-api-tree-view/addToSelectedEndpointsCommand';
import { FilterDescriptionCommand } from './commands/open-api-tree-view/filterDescriptionCommand';
import { OpenDocumentationPageCommand } from './commands/open-api-tree-view/openDocumentationPageCommand';
import { RemoveAllFromSelectedEndpointsCommand } from './commands/open-api-tree-view/removeAllFromSelectedEndpointsCommand';
import { RemoveFromSelectedEndpointsCommand } from './commands/open-api-tree-view/removeFromSelectedEndpointsCommand';
import { KIOTA_WORKSPACE_FILE, dependenciesInfo, extensionId, statusBarCommandId, treeViewFocusCommand, treeViewId } from "./constants";
import { DependenciesViewProvider } from "./dependenciesViewProvider";
import { GenerationType, KiotaGenerationLanguage, KiotaPluginType } from "./enums";
import { ExtensionSettings, getExtensionSettings } from "./extensionSettings";
import { generateClient } from "./generateClient";
import { generatePlugin } from "./generatePlugin";
import { getKiotaVersion } from "./getKiotaVersion";
import { getLanguageInformation, getLanguageInformationForDescription } from "./getLanguageInformation";
import {
  ClientOrPluginProperties,
  ConsumerOperation,
  KiotaLogEntry,
  LogLevel,
  generationLanguageToString,
  getLogEntriesForLevel,
} from "./kiotaInterop";
import { checkForLockFileAndPrompt } from "./migrateFromLockFile";
import { OpenApiTreeNode, OpenApiTreeProvider } from "./openApiTreeProvider";
import { searchDescription } from "./searchDescription";
import { GenerateState, generateSteps, searchSteps } from "./steps";
import { updateClients } from "./updateClients";
import {
  getSanitizedString, getWorkspaceJsonDirectory, getWorkspaceJsonPath,
  isClientType, isPluginType, parseGenerationLanguage,
  parseGenerationType, parsePluginType, updateTreeViewIcons
} from "./util";
import { IntegrationParams, isDeeplinkEnabled, transformToGenerationConfig, validateDeepLinkQueryParams } from './utilities/deep-linking';
import { openTreeViewWithProgress } from './utilities/progress';
import { confirmOverride } from './utilities/regeneration';
import { loadTreeView } from "./workspaceTreeProvider";

let kiotaStatusBarItem: vscode.StatusBarItem;
let kiotaOutputChannel: vscode.LogOutputChannel;
let clientOrPluginKey: string;
let clientOrPluginObject: ClientOrPluginProperties;
let workspaceGenerationType: string;
let config: Partial<GenerateState>;
interface GeneratedOutputState {
  outputPath: string;
  clientClassName: string;
}

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export async function activate(
  context: vscode.ExtensionContext
): Promise<void> {
  kiotaOutputChannel = vscode.window.createOutputChannel("Kiota", {
    log: true,
  });
  const openApiTreeProvider = new OpenApiTreeProvider(context, () => getExtensionSettings(extensionId));
  const dependenciesInfoProvider = new DependenciesViewProvider(
    context.extensionUri
  );
  const reporter = new TelemetryReporter(context.extension.packageJSON.telemetryInstrumentationKey);

  const migrateFromLockFileCommand = new MigrateFromLockFileCommand(context);
  const addAllToSelectedEndpointsCommand = new AddAllToSelectedEndpointsCommand(openApiTreeProvider);
  const addToSelectedEndpointsCommand = new AddToSelectedEndpointsCommand(openApiTreeProvider);
  const removeAllFromSelectedEndpointsCommand = new RemoveAllFromSelectedEndpointsCommand(openApiTreeProvider);
  const removeFromSelectedEndpointsCommand = new RemoveFromSelectedEndpointsCommand(openApiTreeProvider);
  const filterDescriptionCommand = new FilterDescriptionCommand(openApiTreeProvider);
  const openDocumentationPageCommand = new OpenDocumentationPageCommand();
  const editPathsCommand = new EditPathsCommand(openApiTreeProvider);

  await loadTreeView(context);
  await checkForLockFileAndPrompt(context);
  let codeLensProvider = new CodeLensProvider();
  let deepLinkParams: Partial<IntegrationParams> = {};
  context.subscriptions.push(
    vscode.window.registerUriHandler({
      handleUri: async (uri: vscode.Uri) => {
        if (uri.path === "/") {
          return;
        }
        const queryParameters = getQueryParameters(uri);
        if (uri.path.toLowerCase() === "/opendescription") {
          let errorsArray: string[];
          [deepLinkParams, errorsArray] = validateDeepLinkQueryParams(queryParameters);
          reporter.sendTelemetryEvent("DeepLink.OpenDescription initialization status", {
            "queryParameters": JSON.stringify(queryParameters),
            "validationErrors": errorsArray.join(", ")
          });

          if (deepLinkParams.descriptionurl) {
            await openTreeViewWithProgress(() => openApiTreeProvider.setDescriptionUrl(deepLinkParams.descriptionurl!));
            return;
          }
        }
        void vscode.window.showErrorMessage(
          vscode.l10n.t("Invalid URL, please check the documentation for the supported URLs")
        );
      }
    }),

    vscode.languages.registerCodeLensProvider('json', codeLensProvider),
    reporter,
    registerCommandWithTelemetry(reporter,
      `${extensionId}.selectLock`,
      (x) => loadWorkspaceFile(x, openApiTreeProvider)
    ),
    registerCommandWithTelemetry(reporter, statusBarCommandId, async () => {
      const yesAnswer = vscode.l10n.t("Yes");
      const response = await vscode.window.showInformationMessage(
        vscode.l10n.t("Open installation instructions for kiota?"),
        yesAnswer,
        vscode.l10n.t("No")
      );
      if (response === yesAnswer) {
        await vscode.env.openExternal(vscode.Uri.parse("https://aka.ms/get/kiota"));
      }
    }),
    vscode.window.registerWebviewViewProvider(
      dependenciesInfo,
      dependenciesInfoProvider
    ),
    vscode.window.registerTreeDataProvider(treeViewId, openApiTreeProvider),
    registerCommandWithTelemetry(reporter, openDocumentationPageCommand.getName(), async (openApiTreeNode: OpenApiTreeNode) => await openDocumentationPageCommand.execute(openApiTreeNode)),
    registerCommandWithTelemetry(reporter, addToSelectedEndpointsCommand.getName(), async (openApiTreeNode: OpenApiTreeNode) => await addToSelectedEndpointsCommand.execute(openApiTreeNode)),
    registerCommandWithTelemetry(reporter, addAllToSelectedEndpointsCommand.getName(), async (openApiTreeNode: OpenApiTreeNode) => await addAllToSelectedEndpointsCommand.execute(openApiTreeNode)),
    registerCommandWithTelemetry(reporter, removeFromSelectedEndpointsCommand.getName(), async (openApiTreeNode: OpenApiTreeNode) => await removeFromSelectedEndpointsCommand.execute(openApiTreeNode)),
    registerCommandWithTelemetry(reporter, removeAllFromSelectedEndpointsCommand.getName(), async (openApiTreeNode: OpenApiTreeNode) => await removeAllFromSelectedEndpointsCommand.execute(openApiTreeNode)),

    registerCommandWithTelemetry(reporter,
      `${treeViewId}.generateClient`,
      async () => {
        const selectedPaths = openApiTreeProvider.getSelectedPaths();
        if (selectedPaths.length === 0) {
          await vscode.window.showErrorMessage(
            vscode.l10n.t("No endpoints selected, select endpoints first")
          );
          return;
        }

        let languagesInformation = await getLanguageInformation(context);
        let availableStateInfo: Partial<GenerateState>;
        if (isDeeplinkEnabled(deepLinkParams)) {
          if (!deepLinkParams.name && openApiTreeProvider.apiTitle) {
            deepLinkParams.name = getSanitizedString(openApiTreeProvider.apiTitle);
          }
          availableStateInfo = transformToGenerationConfig(deepLinkParams);
        } else {
          const pluginName = getSanitizedString(openApiTreeProvider.apiTitle);
          availableStateInfo = {
            clientClassName: openApiTreeProvider.clientClassName,
            clientNamespaceName: openApiTreeProvider.clientNamespaceName,
            language: openApiTreeProvider.language,
            outputPath: openApiTreeProvider.outputPath,
            pluginName
          };
        }
        config = await generateSteps(
          availableStateInfo,
          languagesInformation,
          deepLinkParams
        );
        const generationType = parseGenerationType(config.generationType);
        const outputPath = typeof config.outputPath === "string"
          ? config.outputPath
          : "./output";
        await showUpgradeWarningMessage(outputPath, context);
        if (!openApiTreeProvider.descriptionUrl) {
          await vscode.window.showErrorMessage(
            vscode.l10n.t("No description found, select a description first")
          );
          return;
        }

        const settings = getExtensionSettings(extensionId);
        workspaceGenerationType = config.generationType as string;
        let result;
        switch (generationType) {
          case GenerationType.Client:
            result = await generateClientAndRefreshUI(config, settings, outputPath, selectedPaths);
            break;
          case GenerationType.Plugin:
            result = await generatePluginAndRefreshUI(config, settings, outputPath, selectedPaths);
            break;
          case GenerationType.ApiManifest:
            result = await generateManifestAndRefreshUI(config, settings, outputPath, selectedPaths);
            break;
          default:
            await vscode.window.showErrorMessage(
              vscode.l10n.t("Invalid generation type")
            );
            return;
        }
        if (result && getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length === 0) {
          // Save state before opening the new window
          const outputState = {
            outputPath,
            config,
            clientClassName: config.clientClassName || config.pluginName
          };
          void context.workspaceState.update('generatedOutput', outputState as GeneratedOutputState);

          const pathOfSpec = path.join(outputPath, `${outputState.clientClassName?.toLowerCase()}-openapi.yml`);
          const pathPluginManifest = path.join(outputPath, `${outputState.clientClassName?.toLowerCase()}-apiplugin.json`);
          if (deepLinkParams.source && deepLinkParams.source.toLowerCase() === 'ttk') {
            try {
              await vscode.commands.executeCommand(
                'fx-extension.createprojectfromkiota',
                [
                  pathOfSpec,
                  pathPluginManifest,
                  deepLinkParams.ttkContext ? deepLinkParams.ttkContext : undefined
                ]
              );
              openApiTreeProvider.closeDescription();
              await updateTreeViewIcons(treeViewId, false);
            } catch (error) {
              reporter.sendTelemetryEvent("DeepLinked fx-extension.createprojectfromkiota", {
                "error": JSON.stringify(error)
              });
            }
          } else {
            if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
              await vscode.commands.executeCommand('vscode.openFolder', vscode.Uri.file(config.workingDirectory ?? getWorkspaceJsonDirectory()), true);
            } else {
              await displayGenerationResults(context, openApiTreeProvider, config);
            }
          }

          deepLinkParams = {};  // Clear the state after the generation
        }
      }
    ),
    vscode.workspace.onDidChangeWorkspaceFolders(async () => {
      const generatedOutput = context.workspaceState.get<GeneratedOutputState>('generatedOutput');
      if (generatedOutput) {
        const { outputPath } = generatedOutput;
        await displayGenerationResults(context, openApiTreeProvider, config);
        // Clear the state 
        void context.workspaceState.update('generatedOutput', undefined);
      }
    }),
    registerCommandWithTelemetry(
      reporter,
      `${treeViewId}.searchOrOpenApiDescription`,
      async (
        searchParams: Partial<IntegrationParams> = {}
      ) => {
        // set deeplink params if exists
        if (Object.keys(searchParams).length > 0) {
          let errorsArray: string[];
          [deepLinkParams, errorsArray] = validateDeepLinkQueryParams(searchParams);
          reporter.sendTelemetryEvent("DeepLinked searchOrOpenApiDescription", {
            "searchParameters": JSON.stringify(searchParams),
            "validationErrors": errorsArray.join(", ")
          });
        }

        // proceed to enable loading of openapi description
        const yesAnswer = vscode.l10n.t("Yes, override it");
        if (!openApiTreeProvider.isEmpty() && openApiTreeProvider.hasChanges()) {
          const response = await vscode.window.showWarningMessage(
            vscode.l10n.t(
              "Before adding a new API description, consider that your changes and current selection will be lost."),
            yesAnswer,
            vscode.l10n.t("Cancel")
          );
          if (response !== yesAnswer) {
            return;
          }
        }
        const config = await searchSteps(x => vscode.window.withProgress({
          location: vscode.ProgressLocation.Notification,
          cancellable: false,
          title: vscode.l10n.t("Searching...")
        }, (progress, _) => {
          const settings = getExtensionSettings(extensionId);
          return searchDescription(context, x, settings.clearCache);
        }));
        if (config.descriptionPath) {
          await openTreeViewWithProgress(() => openApiTreeProvider.setDescriptionUrl(config.descriptionPath!));
        }
      }
    ),
    registerCommandWithTelemetry(reporter, `${treeViewId}.closeDescription`, async () => {
      const yesAnswer = vscode.l10n.t("Yes");
      const response = await vscode.window.showInformationMessage(
        vscode.l10n.t("Do you want to remove this API description?"),
        yesAnswer,
        vscode.l10n.t("No")
      );
      if (response === yesAnswer) {
        openApiTreeProvider.closeDescription();
        await updateTreeViewIcons(treeViewId, false);
      }
    }
    ),
    registerCommandWithTelemetry(reporter, filterDescriptionCommand.getName(), async () => await filterDescriptionCommand.execute()),
    registerCommandWithTelemetry(reporter, editPathsCommand.getName(), async (clientKey: string, clientObject: ClientOrPluginProperties, generationType: string) => {
      clientOrPluginKey = clientKey;
      clientOrPluginObject = clientObject;
      workspaceGenerationType = generationType;
      await editPathsCommand.execute({ clientKey, clientObject });
    }),

    registerCommandWithTelemetry(reporter, `${treeViewId}.regenerateButton`, async () => {
      const regenerate = await confirmOverride();
      if (!regenerate) {
        return;
      }

      if (!clientOrPluginKey || clientOrPluginKey === '') {
        clientOrPluginKey = config.clientClassName || config.pluginName || '';
      }
      if (!config) {
        config = {
          outputPath: clientOrPluginObject.outputPath,
          clientClassName: clientOrPluginKey,
        };
      }
      const settings = getExtensionSettings(extensionId);
      const selectedPaths = openApiTreeProvider.getSelectedPaths();
      if (selectedPaths.length === 0) {
        await vscode.window.showErrorMessage(
          vscode.l10n.t("No endpoints selected, select endpoints first")
        );
        return;
      }
      if (isClientType(workspaceGenerationType)) {
        await regenerateClient(clientOrPluginKey, config, settings, selectedPaths);
      }
      if (isPluginType(workspaceGenerationType)) {
        await regeneratePlugin(clientOrPluginKey, config, settings, selectedPaths);
      }
    }),
    registerCommandWithTelemetry(reporter, `${extensionId}.regenerate`, async (clientKey: string, clientObject: ClientOrPluginProperties, generationType: string) => {
      const regenerate = await confirmOverride();
      if (!regenerate) {
        return;
      }

      const settings = getExtensionSettings(extensionId);
      const workspaceJson = vscode.workspace.textDocuments.find(doc => doc.fileName.endsWith(KIOTA_WORKSPACE_FILE));
      if (workspaceJson && workspaceJson.isDirty) {
        await vscode.window.showInformationMessage(
          vscode.l10n.t("Please save the workspace.json file before re-generation."),
          vscode.l10n.t("OK")
        );
        return;
      }
      if (isClientType(generationType)) {
        await regenerateClient(clientKey, clientObject, settings);
      }
      if (isPluginType(generationType)) {
        await regeneratePlugin(clientKey, clientObject, settings);
      }
    }),
    registerCommandWithTelemetry(reporter, migrateFromLockFileCommand.getName(), async (uri: vscode.Uri) => await migrateFromLockFileCommand.execute(uri)),
  );

  async function generateManifestAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]): Promise<KiotaLogEntry[] | undefined> {
    const pluginTypes = KiotaPluginType.ApiManifest;
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Generating manifest...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        context,
        openApiTreeProvider.descriptionUrl,
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
      reporter.sendRawTelemetryEvent(`${extensionId}.generateManifest.completed`, {
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
      void vscode.window.showInformationMessage(vscode.l10n.t('Generation completed successfully.'));
    }
    return result;
  }
  async function generatePluginAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]): Promise<KiotaLogEntry[] | undefined> {
    const pluginTypes = Array.isArray(config.pluginTypes) ? parsePluginType(config.pluginTypes) : [KiotaPluginType.ApiPlugin];
    const result = await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Generating plugin...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        context,
        openApiTreeProvider.descriptionUrl,
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
      const isttkIntegration = deepLinkParams.source && deepLinkParams.source.toLowerCase() === 'ttk';
      if (!isttkIntegration) {
        void vscode.window.showInformationMessage(vscode.l10n.t('Plugin generated successfully.'));
      }
    }
    return result;
  }
  async function generateClientAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]): Promise<KiotaLogEntry[] | undefined> {
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
        context,
        openApiTreeProvider.descriptionUrl,
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
      reporter.sendRawTelemetryEvent(`${extensionId}.generateClient.completed`, {
        "language": generationLanguageToString(language),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      return result;
    });

    let languagesInformation = await getLanguageInformationForDescription(
      context,
      openApiTreeProvider.descriptionUrl,
      settings.clearCache
    );
    if (languagesInformation) {
      dependenciesInfoProvider.update(languagesInformation, language);
      await vscode.commands.executeCommand(treeViewFocusCommand);
    }
    if (result) {
      const isSuccess = await checkForSuccess(result);
      if (!isSuccess) {
        await exportLogsAndShowErrors(result);
      }
      void vscode.window.showInformationMessage(vscode.l10n.t('Generation completed successfully.'));
    }
    return result;
  }

  async function displayGenerationResults(context: vscode.ExtensionContext, openApiTreeProvider: OpenApiTreeProvider, config: any) {
    const clientNameOrPluginName = config.clientClassName || config.pluginName;
    openApiTreeProvider.refreshView();
    const workspaceJsonPath = getWorkspaceJsonPath();
    await loadWorkspaceFile({ fsPath: workspaceJsonPath }, openApiTreeProvider, clientNameOrPluginName);
    await vscode.commands.executeCommand('kiota.workspace.refresh');
    openApiTreeProvider.resetInitialState();
    await updateTreeViewIcons(treeViewId, false, true);
  }
  async function regenerateClient(clientKey: string, clientObject: any, settings: ExtensionSettings, selectedPaths?: string[]): Promise<void> {
    const language =
      typeof clientObject.language === "string"
        ? parseGenerationLanguage(clientObject.language)
        : KiotaGenerationLanguage.CSharp;
    await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Re-generating client...")
    }, async (progress, _) => {
      const result = await generateClient(
        context,
        clientObject.descriptionLocation ? clientObject.descriptionLocation : openApiTreeProvider.descriptionUrl,
        clientObject.outputPath,
        language,
        selectedPaths ? selectedPaths : clientObject.includePatterns,
        clientObject.excludePatterns ? clientObject.excludePatterns : [],
        clientKey,
        clientObject.clientNamespaceName,
        clientObject.usesBackingStore ? clientObject.usesBackingStore : settings.backingStore,
        true, // clearCache
        true, // cleanOutput
        clientObject.excludeBackwardCompatible ? clientObject.excludeBackwardCompatible : settings.excludeBackwardCompatible,
        clientObject.disabledValidationRules ? clientObject.disabledValidationRules : settings.disableValidationRules,
        settings.languagesSerializationConfiguration[language].serializers,
        settings.languagesSerializationConfiguration[language].deserializers,
        clientObject.structuredMimeTypes ? clientObject.structuredMimeTypes : settings.structuredMimeTypes,
        clientObject.includeAdditionalData ? clientObject.includeAdditionalData : settings.includeAdditionalData,
        ConsumerOperation.Edit
      );
      if (result) {
        const isSuccess = await checkForSuccess(result);
        if (!isSuccess) {
          await exportLogsAndShowErrors(result);
        }
        void vscode.window.showInformationMessage(`Client ${clientKey} re-generated successfully.`);
      }
      return result;
    });

    openApiTreeProvider.resetInitialState();
  }
  async function regeneratePlugin(clientKey: string, clientObject: any, settings: ExtensionSettings, selectedPaths?: string[]) {
    const pluginTypes = Array.isArray(clientObject.types) ? parsePluginType(clientObject.types) : [KiotaPluginType.ApiPlugin];
    await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Re-generating plugin...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        context,
        clientObject.descriptionLocation ? clientObject.descriptionLocation : openApiTreeProvider.descriptionUrl,
        clientObject.outputPath,
        pluginTypes,
        selectedPaths ? selectedPaths : clientObject.includePatterns,
        [],
        clientKey,
        settings.clearCache,
        false,
        settings.disableValidationRules,
        ConsumerOperation.Edit
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
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
        void vscode.window.showInformationMessage(`Plugin ${clientKey} re-generated successfully.`);
      }
      return result;
    });
    openApiTreeProvider.resetInitialState();
  }

  // create a new status bar item that we can now manage
  kiotaStatusBarItem = vscode.window.createStatusBarItem(
    vscode.StatusBarAlignment.Right,
    100
  );
  kiotaStatusBarItem.command = statusBarCommandId;
  context.subscriptions.push(kiotaStatusBarItem);

  // update status bar item once at start
  await updateStatusBarItem(context);
  let disposable = vscode.commands.registerCommand(
    `${extensionId}.updateClients`,
    async () => {
      if (
        !vscode.workspace.workspaceFolders ||
        vscode.workspace.workspaceFolders.length === 0
      ) {
        await vscode.window.showErrorMessage(
          vscode.l10n.t("No workspace folder found, open a folder first")
        );
        return;
      }
      const existingworkspaceFileUris = await vscode.workspace.findFiles(`**/${KIOTA_WORKSPACE_FILE}`);
      if (existingworkspaceFileUris.length > 0) {
        await Promise.all(existingworkspaceFileUris.map(x => path.dirname(x.fsPath)).map(x => showUpgradeWarningMessage(x, context)));
      }
      await updateStatusBarItem(context);
      try {
        kiotaOutputChannel.clear();
        kiotaOutputChannel.show();
        kiotaOutputChannel.info(
          vscode.l10n.t("updating client with path {path}", {
            path: vscode.workspace.workspaceFolders[0].uri.fsPath,
          })
        );
        const res = await vscode.window.withProgress({
          location: vscode.ProgressLocation.Notification,
          cancellable: false,
          title: vscode.l10n.t("Updating clients...")
        }, (progress, _) => {
          const settings = getExtensionSettings(extensionId);
          return updateClients(context, settings.cleanOutput, settings.clearCache);
        });
        if (res) {
          await exportLogsAndShowErrors(res);
        }
      } catch (error) {
        kiotaOutputChannel.error(
          vscode.l10n.t("error updating the clients {error}"),
          error
        );
        await vscode.window.showErrorMessage(
          vscode.l10n.t("error updating the clients {error}"),
          error as string
        );
      }
    }
  );

  context.subscriptions.push(disposable);
}

function registerCommandWithTelemetry(reporter: TelemetryReporter, command: string, callback: (...args: any[]) => any, thisArg?: any): vscode.Disposable {
  return vscode.commands.registerCommand(command, (...args: any[]) => {
    const splatCommand = command.split('/');
    const eventName = splatCommand[splatCommand.length - 1];
    reporter.sendTelemetryEvent(eventName);
    return callback.apply(thisArg, args);
  }, thisArg);
}

async function showUpgradeWarningMessage(clientPath: string, context: vscode.ExtensionContext): Promise<void> {
  const kiotaVersion = context.extension.packageJSON.kiotaVersion.toLocaleLowerCase();
  const workspaceFilePath = path.join(clientPath, KIOTA_WORKSPACE_FILE);
  if (!fs.existsSync(workspaceFilePath)) {
    return;
  }
  const workspaceFileData = await vscode.workspace.fs.readFile(vscode.Uri.file(workspaceFilePath));
  const workspaceFile = JSON.parse(workspaceFileData.toString()) as { kiotaVersion: string };
  const clientVersion = workspaceFile.kiotaVersion.toLocaleLowerCase();
  if (clientVersion.toLocaleLowerCase() !== kiotaVersion) {
    await vscode.window.showWarningMessage(vscode.l10n.t("Client will be upgraded from version {0} to {1}, upgrade your dependencies", clientVersion, kiotaVersion));
  }
}

async function loadWorkspaceFile(node: { fsPath: string }, openApiTreeProvider: OpenApiTreeProvider, clientOrPluginName?: string): Promise<void> {
  await openTreeViewWithProgress(() => openApiTreeProvider.loadWorkspaceFile(node.fsPath, clientOrPluginName));
  await updateTreeViewIcons(treeViewId, true);
}

async function loadEditPaths(clientOrPluginKey: string, clientObject: any, openApiTreeProvider: OpenApiTreeProvider): Promise<void> {
  await openTreeViewWithProgress(() => openApiTreeProvider.loadEditPaths(clientOrPluginKey, clientObject));
}

async function exportLogsAndShowErrors(result: KiotaLogEntry[]): Promise<void> {
  const errorMessages = result
    ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error)
    : [];

  result.forEach((element) => {
    logFromLogLevel(element);
  });
  if (errorMessages.length > 0) {
    await Promise.all(errorMessages.map((element) => {
      return vscode.window.showErrorMessage(element.message);
    }));
  }
}

function logFromLogLevel(entry: KiotaLogEntry): void {
  switch (entry.level) {
    case LogLevel.critical:
    case LogLevel.error:
      kiotaOutputChannel.error(entry.message);
      break;
    case LogLevel.warning:
      kiotaOutputChannel.warn(entry.message);
      break;
    case LogLevel.debug:
      kiotaOutputChannel.debug(entry.message);
      break;
    case LogLevel.trace:
      kiotaOutputChannel.trace(entry.message);
      break;
    default:
      kiotaOutputChannel.info(entry.message);
      break;
  }
}

async function updateStatusBarItem(context: vscode.ExtensionContext): Promise<void> {
  try {
    const version = await getKiotaVersion(context, kiotaOutputChannel);
    if (!version) {
      throw new Error("kiota not found");
    }
    kiotaStatusBarItem.text = `$(extensions-info-message) kiota ${version}`;
  } catch (error) {
    kiotaStatusBarItem.text = `$(extensions-warning-message) kiota ${vscode.l10n.t(
      "not found"
    )}`;
    kiotaStatusBarItem.backgroundColor = new vscode.ThemeColor(
      "statusBarItem.errorBackground"
    );
  }
  kiotaStatusBarItem.show();
}

function getQueryParameters(uri: vscode.Uri): Record<string, string> {
  const query = uri.query;
  if (!query) {
    return {};
  }
  const queryParameters = (query.startsWith('?') ? query.substring(1) : query).split("&");
  const parameters = {} as Record<string, string>;
  queryParameters.forEach((element) => {
    const keyValue = element.split("=");
    parameters[keyValue[0].toLowerCase()] = decodeURIComponent(keyValue[1]);
  });
  return parameters;
}
async function checkForSuccess(results: KiotaLogEntry[]) {
  for (const result of results) {
    if (result && result.message) {
      if (result.message.includes("Generation completed successfully")) {
        return true;
      }
    }
  }
  return false;
}


// This method is called when your extension is deactivated
export function deactivate() { }
