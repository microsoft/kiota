// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from "vscode";
import TelemetryReporter from '@vscode/extension-telemetry';
import * as path from 'path';
import * as fs from 'fs';
import { OpenApiTreeNode, OpenApiTreeProvider } from "./openApiTreeProvider";
import {
  ConsumerOperation,
  generationLanguageToString,
  getLogEntriesForLevel,
  KiotaGenerationLanguage,
  KiotaLogEntry,
  KiotaPluginType,
  LanguageInformation,
  LogLevel,
  parseGenerationLanguage,
  parsePluginType,
} from "./kiotaInterop";
import { GenerateState, GenerationType, filterSteps, generateSteps, openManifestSteps, openSteps, parseGenerationType, searchLockSteps, searchSteps, selectApiManifestKey } from "./steps";
import { getKiotaVersion } from "./getKiotaVersion";
import { searchDescription } from "./searchDescription";
import { generateClient } from "./generateClient";
import { getLanguageInformation, getLanguageInformationForDescription } from "./getLanguageInformation";
import { DependenciesViewProvider } from "./dependenciesViewProvider";
import { updateClients } from "./updateClients";
import { ApiManifest } from "./apiManifest";
import { ExtensionSettings, getExtensionSettings } from "./extensionSettings";
import {  KiotaWorkspace } from "./workspaceTreeProvider";
import { generatePlugin } from "./generatePlugin";
import { CodeLensProvider } from "./codelensProvider";

let kiotaStatusBarItem: vscode.StatusBarItem;
let kiotaOutputChannel: vscode.LogOutputChannel;
const extensionId = "kiota";
const focusCommandId = ".focus";
const statusBarCommandId = `${extensionId}.status`;
const treeViewId = `${extensionId}.openApiExplorer`;
const treeViewFocusCommand = `${treeViewId}${focusCommandId}`;
const dependenciesInfo = `${extensionId}.dependenciesInfo`;
export const kiotaLockFile = "workspace.json";

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
  new KiotaWorkspace(context);
  let codeLensProvider = new CodeLensProvider();
  context.subscriptions.push(
    vscode.window.registerUriHandler({
      handleUri: async (uri: vscode.Uri) => {
        if (uri.path === "/") {
          return;
        }
        const queryParameters = getQueryParameters(uri);
        if (uri.path.toLowerCase() === "/opendescription") {
          reporter.sendTelemetryEvent("DeepLink.OpenDescription");
          const descriptionUrl = queryParameters["descriptionurl"];
          if (descriptionUrl) {
            await openTreeViewWithProgress(() => openApiTreeProvider.setDescriptionUrl(descriptionUrl));
            return;
          }
        }
        if (uri.path.toLowerCase() === "/openmanifest") {
          reporter.sendTelemetryEvent("DeepLink.OpenManifest");
          const manifestUrl = queryParameters["manifesturl"];
          const manifestContent = queryParameters["manifestcontent"];
          const apiIdentifier = queryParameters["apiidentifier"];
          const fromClipboard = queryParameters["fromclipboard"];
          if (manifestUrl) {
            await openTreeViewWithProgress(async () => {
              const logs = await openApiTreeProvider.loadManifestFromUri(manifestUrl, apiIdentifier);
              await exportLogsAndShowErrors(logs);
            });
            return;
          } else if (manifestContent) {
            await openTreeViewWithProgress(async () => {
              const logs = await openApiTreeProvider.loadManifestFromContent(manifestContent, apiIdentifier);
              await exportLogsAndShowErrors(logs);
            });
            return;
          } else if (fromClipboard.toLowerCase() === "true") {
            await openManifestFromClipboard(openApiTreeProvider, apiIdentifier!);
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
      `${treeViewId}.openFile`,
      async () => {
        const lockFilePath = await searchLockSteps();
        if (lockFilePath?.lockFilePath) {
          await loadLockFile(lockFilePath.lockFilePath, openApiTreeProvider);
        }
      }),
    registerCommandWithTelemetry(reporter, 
      `${extensionId}.selectLock`,
      (x) => loadLockFile(x, openApiTreeProvider)
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
    registerCommandWithTelemetry(reporter, 
      `${treeViewId}.openDocumentationPage`,
      (x: OpenApiTreeNode) => x.documentationUrl && vscode.env.openExternal(vscode.Uri.parse(x.documentationUrl))
    ),
    registerCommandWithTelemetry(reporter, 
      `${treeViewId}.addToSelectedEndpoints`,
      (x: OpenApiTreeNode) => openApiTreeProvider.select(x, true, false)
    ),
    registerCommandWithTelemetry(reporter, 
      `${treeViewId}.addAllToSelectedEndpoints`,
      (x: OpenApiTreeNode) => openApiTreeProvider.select(x, true, true)
    ),
    registerCommandWithTelemetry(reporter, 
      `${treeViewId}.removeFromSelectedEndpoints`,
      (x: OpenApiTreeNode) => openApiTreeProvider.select(x, false, false)
    ),
    registerCommandWithTelemetry(reporter, 
      `${treeViewId}.removeAllFromSelectedEndpoints`,
      (x: OpenApiTreeNode) => openApiTreeProvider.select(x, false, true)
    ),
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
        if (
          !vscode.workspace.workspaceFolders ||
          vscode.workspace.workspaceFolders.length === 0
        ) {
          await vscode.window.showErrorMessage(
            vscode.l10n.t("No workspace folder found, open a folder first")
          );
          return;
        }
        let languagesInformation = await getLanguageInformation(context);
        const config = await generateSteps(
          {
            clientClassName: openApiTreeProvider.clientClassName,
            clientNamespaceName: openApiTreeProvider.clientNamespaceName,
            language: openApiTreeProvider.language,
            outputPath: openApiTreeProvider.outputPath,
          },
          languagesInformation
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
        switch (generationType) {
          case GenerationType.Client:
            await generateClientAndRefreshUI(config, settings, outputPath, selectedPaths);
          break;
          case GenerationType.Plugin:
            await generatePluginAndRefreshUI(config, settings, outputPath, selectedPaths);
          break;
          case GenerationType.ApiManifest:
            await generateManifestAndRefreshUI(config, settings, outputPath, selectedPaths);
          default:
            await vscode.window.showErrorMessage(
              vscode.l10n.t("Invalid generation type")
            );
          break;
        }
      }
    ),
    registerCommandWithTelemetry(reporter, 
      `${treeViewId}.searchOrOpenApiDescription`,
      async () => {
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
          await vscode.commands.executeCommand('setContext',`${treeViewId}.showIcons`, true);
        }
      }
    ),
    registerCommandWithTelemetry(reporter, `${treeViewId}.closeDescription`, async () =>
      {
      const yesAnswer = vscode.l10n.t("Yes");
      const response = await vscode.window.showInformationMessage(
        vscode.l10n.t("Do you want to remove this API description?"),
        yesAnswer,
        vscode.l10n.t("No")
      );
      if(response === yesAnswer) {
        openApiTreeProvider.closeDescription();
        await vscode.commands.executeCommand('setContext',`${treeViewId}.showIcons`, false);
      }
    }
    ),
    registerCommandWithTelemetry(reporter, `${treeViewId}.filterDescription`,
      async () => {
        await filterSteps(openApiTreeProvider.filter, x => openApiTreeProvider.filter = x);
      }
    ),
    registerCommandWithTelemetry(reporter, 
      `${treeViewId}.openDescription`,
      async () => {
        const openState = await openSteps();
        if (openState.descriptionPath) {
          await openTreeViewWithProgress(() => openApiTreeProvider.setDescriptionUrl(openState.descriptionPath!));
        }
      }
    ),
    registerCommandWithTelemetry(reporter, 
      `${treeViewId}.openManifestPath`,
      async () => {
        const openState = await openManifestSteps();
        if (openState.manifestPath) {
          await openTreeViewWithProgress(() => openApiTreeProvider.loadManifestFromUri(openState.manifestPath!));
        }
      }
    ),
    registerCommandWithTelemetry(reporter, 
      `${treeViewId}.pasteManifest`,
      () => openManifestFromClipboard(openApiTreeProvider, "")
    ),
    registerCommandWithTelemetry(reporter, `${extensionId}.editPaths`, async (clientObject: any) => {
     await loadEditPaths(clientObject, openApiTreeProvider);
     await vscode.commands.executeCommand('setContext',`${treeViewId}.showIcons`, true);
    }),
    registerCommandWithTelemetry(reporter, `${extensionId}.regenerate`, async (clientKey: string, clientObject: any) => {
      const settings = getExtensionSettings(extensionId); 
      await regenerateClient(clientKey, clientObject, settings);
    }),
  );

  async function generateManifestAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]):Promise<void> {
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
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      reporter.sendRawTelemetryEvent(`${extensionId}.generateManifest.completed`, {
        "pluginType": pluginTypes.toString(),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      openApiTreeProvider.closeDescription();
      return result;
    });
    //TODO refresh the kiota workspace
    if (result)
    {
      await exportLogsAndShowErrors(result);
    }
  }
  async function generatePluginAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]):Promise<void> {
    const pluginTypes = typeof config.pluginTypes === 'string' ? parsePluginType(config.pluginTypes) : KiotaPluginType.Microsoft;
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
        [pluginTypes],
        selectedPaths,
        [],
        typeof config.pluginName === "string"
          ? config.pluginName
          : "ApiClient",
        settings.clearCache,
        settings.cleanOutput,
        settings.disableValidationRules,
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      reporter.sendRawTelemetryEvent(`${extensionId}.generatePlugin.completed`, {
        "pluginType": pluginTypes.toString(),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      openApiTreeProvider.closeDescription();
      return result;
    });
    //TODO refresh the kiota workspace
    if (result)
    {
      await exportLogsAndShowErrors(result);
    }
  }
  async function generateClientAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]):Promise<void> {
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
        ConsumerOperation.Add
      );
      const duration = performance.now() - start;
      const errorsCount = result ? getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length : 0;
      reporter.sendRawTelemetryEvent(`${extensionId}.generateClient.completed`, {
        "language": generationLanguageToString(language),
        "errorsCount": errorsCount.toString(),
      }, {
        "duration": duration,
      });
      openApiTreeProvider.closeDescription();
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
    if (typeof config.outputPath === "string" && !openApiTreeProvider.isLockFileLoaded && 
        vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0 &&
        result && getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length === 0) {
      await openApiTreeProvider.loadLockFile(path.join(vscode.workspace.workspaceFolders[0].uri.fsPath, '.kiota', kiotaLockFile));
      //TODO this will need to be updated to refresh the workspace instead
    }
    if (result)
    {
      await exportLogsAndShowErrors(result);
    }
  }
  async function regenerateClient(clientKey: string, clientObject:any, settings: ExtensionSettings): Promise<void> {
    const language =
          typeof clientObject.language === "string"
            ? parseGenerationLanguage(clientObject.language)
            : KiotaGenerationLanguage.CSharp;
            console.log(
              context,
        clientObject.descriptionLocation,
        clientObject.outputPath,
        language,
        clientObject.includePatterns,
        clientObject.excludePatterns,
        clientKey,
        clientObject.clientNamespaceName,
        clientObject.usesBackingStore,
        true, // clearCache
        true, // cleanOutput
        clientObject.excludeBackwardCompatible,
        clientObject.disabledValidationRules,
        settings.languagesSerializationConfiguration[language].serializers,
        settings.languagesSerializationConfiguration[language].deserializers,
        clientObject.structuredMimeTypes,
        clientObject.includeAdditionalData
            );
     await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Generating client...")  
    }, async (progress, _) => {
      const result = await generateClient(
        context,
        clientObject.descriptionLocation,
        clientObject.outputPath,
        language,
        clientObject.includePatterns,
        clientObject.excludePatterns,
        clientKey,
        clientObject.clientNamespaceName,
        clientObject.usesBackingStore,
        true, // clearCache
        true, // cleanOutput
        clientObject.excludeBackwardCompatible,
        clientObject.disabledValidationRules,
        settings.languagesSerializationConfiguration[language].serializers,
        settings.languagesSerializationConfiguration[language].deserializers,
        clientObject.structuredMimeTypes,
        clientObject.includeAdditionalData,
        ConsumerOperation.Edit
    );
    return result;
    });
    
  void vscode.window.showInformationMessage(`Client ${clientKey} regenerated successfully.`);
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
      const existingLockFileUris = await vscode.workspace.findFiles(`**/${kiotaLockFile}`);
      if (existingLockFileUris.length > 0) {
        await Promise.all(existingLockFileUris.map(x => path.dirname(x.fsPath)).map(x => showUpgradeWarningMessage(x, context)));
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
async function openManifestFromClipboard(openApiTreeProvider: OpenApiTreeProvider, apiIdentifier?: string): Promise<void> {
  await openTreeViewWithProgress(async () => {
    let clipBoardContent = await vscode.env.clipboard.readText();
    if (!clipBoardContent) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("No content found in the clipboard")
      );
      return;
    }
    try {
      let deserializedContent: ApiManifest;
      try {
        deserializedContent = JSON.parse(clipBoardContent) as ApiManifest;
        // if it's valid json, it's not base64 encoded
      } catch {
        clipBoardContent = Buffer.from(clipBoardContent, 'base64').toString('utf-8');
        deserializedContent = JSON.parse(clipBoardContent) as ApiManifest;
      }
      if (!apiIdentifier && deserializedContent.apiDependencies && Object.keys(deserializedContent.apiDependencies).length > 1) {
        const apiKeys = Object.keys(deserializedContent.apiDependencies);
        const selectKeyResult = await selectApiManifestKey(apiKeys);
        if (selectKeyResult.selectedKey) {
          apiIdentifier = selectKeyResult.selectedKey;
        }
      }
    } catch (error) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("Invalid content found in the clipboard")
      );
      return;
    }
    const logs = await openApiTreeProvider.loadManifestFromContent(clipBoardContent, apiIdentifier);
    await exportLogsAndShowErrors(logs);
  });
}
function openTreeViewWithProgress<T>(callback: () => Promise<T>): Thenable<T> {
  return vscode.window.withProgress({
    location: vscode.ProgressLocation.Notification,
    cancellable: false,
    title: vscode.l10n.t("Loading...")
  }, async (progress, _) => {
    const result = await callback();
    await vscode.commands.executeCommand(treeViewFocusCommand);
    return result;
  });
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
  const lockFilePath = path.join(clientPath, kiotaLockFile);
  if(!fs.existsSync(lockFilePath)) {
    return;
  }
  const lockFileData = await vscode.workspace.fs.readFile(vscode.Uri.file(lockFilePath));
  const lockFile = JSON.parse(lockFileData.toString()) as {kiotaVersion: string};
  const clientVersion = lockFile.kiotaVersion.toLocaleLowerCase();
  if (clientVersion.toLocaleLowerCase() !== kiotaVersion) {
    await vscode.window.showWarningMessage(vscode.l10n.t("Client will be upgraded from version {0} to {1}, upgrade your dependencies", clientVersion, kiotaVersion));
  }
}

async function loadLockFile(node: { fsPath: string }, openApiTreeProvider: OpenApiTreeProvider): Promise<void> {
  await openTreeViewWithProgress(() => openApiTreeProvider.loadLockFile(node.fsPath));
}

async function loadEditPaths(clientObject: any, openApiTreeProvider: OpenApiTreeProvider): Promise<void> {
  await openTreeViewWithProgress(() => openApiTreeProvider.loadEditPaths(clientObject));
}

async function exportLogsAndShowErrors(result: KiotaLogEntry[]) : Promise<void> {
  const informationMessages = result
    ? getLogEntriesForLevel(result, LogLevel.information)
    : [];
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
  } else {
    await Promise.all(informationMessages.map((element) => {
      return vscode.window.showInformationMessage(element.message);
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

// This method is called when your extension is deactivated
export function deactivate() {}
