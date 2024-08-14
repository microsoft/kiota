// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import TelemetryReporter from '@vscode/extension-telemetry';

import * as path from 'path';
import * as vscode from "vscode";

import { CodeLensProvider } from "./codelensProvider";
import { KiotaStatusCommand } from "./commands/KiotaStatusCommand";
import { OpenApiTreeNodeCommand } from "./commands/OpenApiTreeNodeCommand";

import { GenerateCommand } from './commands/GenerateCommand';
import { GeneratedOutputState } from './commands/GeneratedOutputState';
import { KIOTA_WORKSPACE_FILE, dependenciesInfo, extensionId, statusBarCommandId, treeViewId } from "./constants";
import { DependenciesViewProvider } from "./dependenciesViewProvider";
import { ExtensionSettings, getExtensionSettings } from "./extensionSettings";
import { generateClient } from "./generateClient";
import { generatePlugin } from "./generatePlugin";
import { getKiotaVersion } from "./getKiotaVersion";
import {
  ClientOrPluginProperties,
  ConsumerOperation,
  KiotaGenerationLanguage,
  KiotaLogEntry,
  KiotaPluginType,
  LogLevel,
  getLogEntriesForLevel,
  parseGenerationLanguage,
  parsePluginType
} from "./kiotaInterop";
import { OpenApiTreeNode, OpenApiTreeProvider } from "./openApiTreeProvider";
import { searchDescription } from "./searchDescription";
import { GenerateState, filterSteps, searchSteps } from "./steps";
import { updateClients } from "./updateClients";
import { isClientType, isPluginType, updateTreeViewIcons } from "./util";
import { loadLockFile, openTreeViewWithProgress } from './utilities/file';
import { exportLogsAndShowErrors, kiotaOutputChannel } from './utilities/logging';
import { showUpgradeWarningMessage } from './utilities/messaging';
import { loadTreeView } from "./workspaceTreeProvider";
import { SearchOrOpenApiDescriptionCommand } from './commands/SearchOrOpenApiDescriptionCommand';
import { CloseDescriptionCommand } from './commands/CloseDescriptionCommand';
import { FilterDescriptionCommand } from './commands/FilterDescriptionCommand';

let kiotaStatusBarItem: vscode.StatusBarItem;
let clientOrPluginKey: string;
let clientOrPluginObject: ClientOrPluginProperties;
let workspaceGenerationType: string;
let config: Partial<GenerateState>;


// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export async function activate(
  context: vscode.ExtensionContext
): Promise<void> {
  const openApiTreeProvider = new OpenApiTreeProvider(context, () => getExtensionSettings(extensionId));
  const dependenciesInfoProvider = new DependenciesViewProvider(
    context.extensionUri
  );
  const kiotaStatusCommand = new KiotaStatusCommand();
  const openApiTreeNodeCommand = new OpenApiTreeNodeCommand();
  const generateCommand = new GenerateCommand(context, openApiTreeProvider);
  const searchOrOpenApiDescriptionCommand = new SearchOrOpenApiDescriptionCommand(context, openApiTreeProvider);
  const closeDescriptionCommand = new CloseDescriptionCommand(openApiTreeProvider);
  const filterDescriptionCommand = new FilterDescriptionCommand(openApiTreeProvider);

  const reporter = new TelemetryReporter(context.extension.packageJSON.telemetryInstrumentationKey);
  await loadTreeView(context);
  let codeLensProvider = new CodeLensProvider();
  const handleUri = async (uri: vscode.Uri) => {
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
  };
  context.subscriptions.push(
    vscode.window.registerUriHandler({
      handleUri
    }),

    vscode.languages.registerCodeLensProvider('json', codeLensProvider),
    reporter,
    registerCommandWithTelemetry(reporter,
      `${extensionId}.selectLock`,
      (x) => loadLockFile(x, openApiTreeProvider)
    ),
    registerCommandWithTelemetry(reporter, statusBarCommandId, await kiotaStatusCommand.execute()),
    vscode.window.registerWebviewViewProvider(
      dependenciesInfo,
      dependenciesInfoProvider
    ),
    vscode.window.registerTreeDataProvider(treeViewId, openApiTreeProvider),
    registerCommandWithTelemetry(reporter,
      `${treeViewId}.openDocumentationPage`,
      (x: OpenApiTreeNode) => openApiTreeNodeCommand.execute(x)
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
      `${treeViewId}.generateClient`, () => generateCommand.execute()
    ),
    vscode.workspace.onDidChangeWorkspaceFolders(async () => {
      const generatedOutput = context.workspaceState.get<GeneratedOutputState>('generatedOutput');
      if (generatedOutput) {
        const { outputPath } = generatedOutput;
        await generateCommand.displayGenerationResults(config, outputPath);
        // Clear the state 
        void context.workspaceState.update('generatedOutput', undefined);
      }
    }),
    registerCommandWithTelemetry(reporter, `${treeViewId}.searchOrOpenApiDescription`, () => searchOrOpenApiDescriptionCommand.execute()),
    registerCommandWithTelemetry(reporter, `${treeViewId}.closeDescription`, () => closeDescriptionCommand.execute()),
    registerCommandWithTelemetry(reporter, `${treeViewId}.filterDescription`, () => filterDescriptionCommand.execute()),
    registerCommandWithTelemetry(reporter, `${extensionId}.editPaths`, async (clientKey: string, clientObject: ClientOrPluginProperties, generationType: string) => {
      clientOrPluginKey = clientKey;
      clientOrPluginObject = clientObject;
      workspaceGenerationType = generationType;
      await loadEditPaths(clientOrPluginKey, clientObject, openApiTreeProvider);
      openApiTreeProvider.resetInitialState();
      await updateTreeViewIcons(treeViewId, false, true);
    }),
    registerCommandWithTelemetry(reporter, `${treeViewId}.regenerateButton`, async () => {
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
      return result;
    });
    void vscode.window.showInformationMessage(`Client ${clientKey} re-generated successfully.`);
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
        await Promise.all(existingLockFileUris.map(x => path.dirname(x.fsPath)).map(x => showUpgradeWarningMessage(context, x)));
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

async function loadEditPaths(clientOrPluginKey: string, clientObject: any, openApiTreeProvider: OpenApiTreeProvider): Promise<void> {
  await openTreeViewWithProgress(() => openApiTreeProvider.loadEditPaths(clientOrPluginKey, clientObject));
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
export function deactivate() { }
