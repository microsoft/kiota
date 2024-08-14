// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from "vscode";
import TelemetryReporter from '@vscode/extension-telemetry';
import * as path from 'path';
import * as fs from 'fs';
import { OpenApiTreeNode, OpenApiTreeProvider } from "./openApiTreeProvider";
import {
  ClientOrPluginProperties,
  ConsumerOperation,
  generationLanguageToString,
  getLogEntriesForLevel,
  KiotaGenerationLanguage,
  KiotaLogEntry,
  KiotaPluginType,
  LogLevel,
  parseGenerationLanguage,
  parsePluginType,
} from "./kiotaInterop";
import { GenerateState, GenerationType, filterSteps, generateSteps, parseGenerationType, searchSteps } from "./steps";
import { getKiotaVersion } from "./getKiotaVersion";
import { searchDescription } from "./searchDescription";
import { generateClient } from "./generateClient";
import { getLanguageInformation, getLanguageInformationForDescription } from "./getLanguageInformation";
import { DependenciesViewProvider } from "./dependenciesViewProvider";
import { updateClients } from "./updateClients";
import { ExtensionSettings, getExtensionSettings } from "./extensionSettings";
import { loadTreeView } from "./workspaceTreeProvider";
import { generatePlugin } from "./generatePlugin";
import { CodeLensProvider } from "./codelensProvider";
import { KIOTA_WORKSPACE_FILE, REMIND_ME_LATER_FLAG, dependenciesInfo, extensionId, statusBarCommandId, treeViewFocusCommand, treeViewId } from "./constants";
import { getWorkspaceJsonDirectory, getWorkspaceJsonPath, isClientType, isPluginType, updateTreeViewIcons } from "./util";
import { displayMigrationMessages, migrateFromLockFile } from "./migrateFromLockFile";

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
  await loadTreeView(context);
  await checkForLockFileAndPrompt(context);
  let codeLensProvider = new CodeLensProvider();
  async function checkForLockFileAndPrompt(context: vscode.ExtensionContext) {
    const workspaceFolders = vscode.workspace.workspaceFolders;
    const remindMeLater = context.globalState.get<boolean>(REMIND_ME_LATER_FLAG);

    if (workspaceFolders || remindMeLater) {
      const result = await vscode.window.showInformationMessage(
        vscode.l10n.t("Please migrate your API clients to Kiota workspace."),
        vscode.l10n.t("OK"),
        vscode.l10n.t("Remind me later")
      );

      if (result === vscode.l10n.t("OK")) {
        await context.globalState.update(REMIND_ME_LATER_FLAG, false);

        vscode.window.withProgress({
          location: vscode.ProgressLocation.Notification,
          title: vscode.l10n.t("Migrating API clients..."),
          cancellable: false
        }, async (progress) => {
          progress.report({ increment: 0 });

          try {
            const migrationResult = await migrateFromLockFile(context, workspaceFolders![0].uri.fsPath);

            progress.report({ increment: 100 });

            if (migrationResult && migrationResult.length > 0) {
              displayMigrationMessages(migrationResult);
            } else {
              vscode.window.showWarningMessage(vscode.l10n.t("Migration completed, but no changes were detected."));
            }
          } catch (error) {
            vscode.window.showErrorMessage(vscode.l10n.t(`Migration failed: ${error}`));
          }
        });
      } else if (result === vscode.l10n.t("Remind me later")) {
        await context.globalState.update(REMIND_ME_LATER_FLAG, true);
      }
    }
  };
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
        void vscode.window.showErrorMessage(
          vscode.l10n.t("Invalid URL, please check the documentation for the supported URLs")
        );
      }
    }),
    
    vscode.languages.registerCodeLensProvider('json', codeLensProvider),
    reporter,
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
    
        let languagesInformation = await getLanguageInformation(context);
        config = await generateSteps(
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
          void context.workspaceState.update('generatedOutput', {
            outputPath,
            config,
            clientClassName: config.clientClassName || config.pluginName
          } as GeneratedOutputState);
          if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
            await vscode.commands.executeCommand('vscode.openFolder', vscode.Uri.file(config.workingDirectory ?? getWorkspaceJsonDirectory()), true);
          } else {
            await displayGenerationResults(context, openApiTreeProvider, config, outputPath);
          }
        }
      }
    ),
    vscode.workspace.onDidChangeWorkspaceFolders(async () => {
      const generatedOutput = context.workspaceState.get<GeneratedOutputState>('generatedOutput');
      if (generatedOutput) {
        const { outputPath} = generatedOutput;
        await displayGenerationResults(context, openApiTreeProvider, config, outputPath);
        // Clear the state 
        void context.workspaceState.update('generatedOutput', undefined);
      }
    }),
    registerCommandWithTelemetry(reporter, 
      `${treeViewId}.searchOrOpenApiDescription`,
      async () => {
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
        await updateTreeViewIcons(treeViewId, false);
      }
    }
    ),
    registerCommandWithTelemetry(reporter, `${treeViewId}.filterDescription`,
      async () => {
        await filterSteps(openApiTreeProvider.filter, x => openApiTreeProvider.filter = x);
      }
    ),
    registerCommandWithTelemetry(reporter, `${extensionId}.editPaths`, async (clientKey: string, clientObject: ClientOrPluginProperties, generationType: string) => {
     clientOrPluginKey = clientKey;
     clientOrPluginObject = clientObject;
     workspaceGenerationType = generationType;
     await loadEditPaths(clientOrPluginKey, clientObject, openApiTreeProvider);
     openApiTreeProvider.resetInitialState();
     await updateTreeViewIcons(treeViewId, false, true);
    }),
    registerCommandWithTelemetry(reporter,`${treeViewId}.regenerateButton`, async () => {
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
    if(isClientType(workspaceGenerationType)) {
      await regenerateClient(clientOrPluginKey, config, settings, selectedPaths);    
    }
    if (isPluginType(workspaceGenerationType)) {
      await regeneratePlugin(clientOrPluginKey, config, settings, selectedPaths);
    }
    }),
    registerCommandWithTelemetry(reporter, `${extensionId}.regenerate`, async (clientKey: string, clientObject: ClientOrPluginProperties, generationType: string) => {
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
  );

  async function generateManifestAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]):Promise<KiotaLogEntry[]| undefined> {
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
    if (result)
    {
      await exportLogsAndShowErrors(result);
    }
    return result;
  }
  async function generatePluginAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]):Promise<KiotaLogEntry[] | undefined> {
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
    if (result)
    {
      await exportLogsAndShowErrors(result);
    }
    return result;
  }
  async function generateClientAndRefreshUI(config: Partial<GenerateState>, settings: ExtensionSettings, outputPath: string, selectedPaths: string[]):Promise<KiotaLogEntry[] | undefined> {
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
    if (result)
    {
      await exportLogsAndShowErrors(result);
    }
    return result;
  }

  async function displayGenerationResults(context: vscode.ExtensionContext, openApiTreeProvider: OpenApiTreeProvider, config: any, outputPath: string) {
    const clientNameOrPluginName = config.clientClassName || config.pluginName;
    openApiTreeProvider.refreshView();
    const workspaceJsonPath = getWorkspaceJsonPath();
    await loadLockFile({fsPath: workspaceJsonPath}, openApiTreeProvider, clientNameOrPluginName );
    await vscode.commands.executeCommand('kiota.workspace.refresh');
    openApiTreeProvider.resetInitialState();
    await updateTreeViewIcons(treeViewId, false, true);
  }
  async function regenerateClient(clientKey: string, clientObject:any, settings: ExtensionSettings,  selectedPaths?: string[]): Promise<void> {
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
        clientObject.descriptionLocation ? clientObject.descriptionLocation: openApiTreeProvider.descriptionUrl,
        clientObject.outputPath,
        language,
        selectedPaths ? selectedPaths : clientObject.includePatterns,
        clientObject.excludePatterns ? clientObject.excludePatterns : [],
        clientKey,
        clientObject.clientNamespaceName,
        clientObject.usesBackingStore ? clientObject.usesBackingStore : settings.backingStore,
        true, // clearCache
        true, // cleanOutput
        clientObject.excludeBackwardCompatible ? clientObject.excludeBackwardCompatible : settings.excludeBackwardCompatible  ,
        clientObject.disabledValidationRules  ? clientObject.disabledValidationRules : settings.disableValidationRules,
        settings.languagesSerializationConfiguration[language].serializers,
        settings.languagesSerializationConfiguration[language].deserializers,
        clientObject.structuredMimeTypes ? clientObject.structuredMimeTypes : settings.structuredMimeTypes,
        clientObject.includeAdditionalData ? clientObject.includeAdditionalData : settings.includeAdditionalData,
        ConsumerOperation.Edit
    );
    return result;
    });
  void vscode.window.showInformationMessage(`Client ${clientKey} re-generated successfully.`);
  openApiTreeProvider.resetInitialState();
  }
  async function regeneratePlugin(clientKey: string, clientObject:any, settings: ExtensionSettings,  selectedPaths?: string[]) {
    const pluginTypes =  Array.isArray(clientObject.types) ? parsePluginType(clientObject.types) : [KiotaPluginType.ApiPlugin];
    await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Re-generating plugin...")
    }, async (progress, _) => {
      const start = performance.now();
      const result = await generatePlugin(
        context,
        clientObject.descriptionLocation ? clientObject.descriptionLocation: openApiTreeProvider.descriptionUrl,
        clientObject.outputPath,
        pluginTypes,
        selectedPaths ? selectedPaths : clientObject.includePatterns,
        [],
        clientKey,
        settings.clearCache,
        settings.cleanOutput,
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
      return result;
    });
    void vscode.window.showInformationMessage(`Plugin ${clientKey} re-generated successfully.`);
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
      const existingLockFileUris = await vscode.workspace.findFiles(`**/${KIOTA_WORKSPACE_FILE}`);
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
  const lockFilePath = path.join(clientPath, KIOTA_WORKSPACE_FILE);
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

async function loadLockFile(node: { fsPath: string }, openApiTreeProvider: OpenApiTreeProvider, clientOrPluginName?: string): Promise<void> {
  await openTreeViewWithProgress(() => openApiTreeProvider.loadLockFile(node.fsPath, clientOrPluginName));
  await updateTreeViewIcons(treeViewId, true);
}

async function loadEditPaths(clientOrPluginKey: string, clientObject: any, openApiTreeProvider: OpenApiTreeProvider): Promise<void> {
  await openTreeViewWithProgress(() => openApiTreeProvider.loadEditPaths(clientOrPluginKey, clientObject));
}

async function exportLogsAndShowErrors(result: KiotaLogEntry[]) : Promise<void> {
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
        void vscode.window.showInformationMessage('Generation completed successfully.');
      }
    }
  }
}


// This method is called when your extension is deactivated
export function deactivate() {}
