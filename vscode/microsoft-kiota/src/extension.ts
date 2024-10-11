// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import TelemetryReporter from '@vscode/extension-telemetry';
import * as vscode from "vscode";

import { CodeLensProvider } from "./codelensProvider";
import { CloseDescriptionCommand } from './commands/closeDescriptionCommand';
import { EditPathsCommand } from './commands/editPathsCommand';
import { GenerateClientCommand } from './commands/generate/generateClientCommand';
import { checkForSuccess, displayGenerationResults } from './commands/generate/generation-util';
import { MigrateFromLockFileCommand } from './commands/migrateFromLockFileCommand';
import { AddAllToSelectedEndpointsCommand } from './commands/open-api-tree-view/addAllToSelectedEndpointsCommand';
import { AddToSelectedEndpointsCommand } from './commands/open-api-tree-view/addToSelectedEndpointsCommand';
import { FilterDescriptionCommand } from './commands/open-api-tree-view/filterDescriptionCommand';
import { OpenDocumentationPageCommand } from './commands/open-api-tree-view/openDocumentationPageCommand';
import { RemoveAllFromSelectedEndpointsCommand } from './commands/open-api-tree-view/removeAllFromSelectedEndpointsCommand';
import { RemoveFromSelectedEndpointsCommand } from './commands/open-api-tree-view/removeFromSelectedEndpointsCommand';
import { SearchOrOpenApiDescriptionCommand } from './commands/open-api-tree-view/search-or-open-api-description/searchOrOpenApiDescriptionCommand';
import { API_MANIFEST_FILE, KIOTA_WORKSPACE_FILE, dependenciesInfo, extensionId, statusBarCommandId, treeViewId } from "./constants";
import { DependenciesViewProvider } from "./dependenciesViewProvider";
import { KiotaGenerationLanguage, KiotaPluginType } from "./enums";
import { ExtensionSettings, getExtensionSettings } from "./extensionSettings";
import { generateClient } from "./generateClient";
import { GeneratedOutputState } from './GeneratedOutputState';
import { generatePlugin } from "./generatePlugin";
import { getKiotaVersion } from "./getKiotaVersion";
import { getGenerationConfiguration, setGenerationConfiguration } from './handlers/configurationHandler';
import { getDeepLinkParams, setDeepLinkParams } from './handlers/deepLinkParamsHandler';
import { getWorkspaceGenerationType, setWorkspaceGenerationType } from './handlers/workspaceGenerationTypeHandler';
import {
  ClientOrPluginProperties,
  ConsumerOperation,
  LogLevel,
  getLogEntriesForLevel
} from "./kiotaInterop";
import { checkForLockFileAndPrompt } from "./migrateFromLockFile";
import { OpenApiTreeNode, OpenApiTreeProvider } from "./openApiTreeProvider";
import { updateClients } from "./updateClients";
import {
  isClientType, isPluginType, parseGenerationLanguage,
  parsePluginType
} from "./util";
import { IntegrationParams, validateDeepLinkQueryParams } from './utilities/deep-linking';
import { loadWorkspaceFile } from './utilities/file';
import { exportLogsAndShowErrors } from './utilities/logging';
import { showUpgradeWarningMessage } from './utilities/messaging';
import { openTreeViewWithProgress } from './utilities/progress';
import { confirmOverride } from './utilities/regeneration';
import { loadTreeView } from "./workspaceTreeProvider";
import { StatusCommand } from './commands/statusCommand';

let kiotaStatusBarItem: vscode.StatusBarItem;
let kiotaOutputChannel: vscode.LogOutputChannel;
export let clientOrPluginKey: string;
export let clientOrPluginObject: ClientOrPluginProperties;
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
  const searchOrOpenApiDescriptionCommand = new SearchOrOpenApiDescriptionCommand(openApiTreeProvider, context);
  const generateClientCommand = new GenerateClientCommand(openApiTreeProvider, context, dependenciesInfoProvider);
  const closeDescriptionCommand = new CloseDescriptionCommand(openApiTreeProvider);
  const statusCommand = new StatusCommand();

  await loadTreeView(context);
  await checkForLockFileAndPrompt(context);
  let codeLensProvider = new CodeLensProvider();
  context.subscriptions.push(
    vscode.window.registerUriHandler({
      handleUri: async (uri: vscode.Uri) => {
        if (uri.path === "/") {
          return;
        }
        const queryParameters = getQueryParameters(uri);
        if (uri.path.toLowerCase() === "/opendescription") {
          let [params, errorsArray] = validateDeepLinkQueryParams(queryParameters);
          setDeepLinkParams(params);
          reporter.sendTelemetryEvent("DeepLink.OpenDescription initialization status", {
            "queryParameters": JSON.stringify(queryParameters),
            "validationErrors": errorsArray.join(", ")
          });

          let deepLinkParams = getDeepLinkParams();
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
    registerCommandWithTelemetry(reporter, statusCommand.getName(), async () => await statusCommand.execute()),
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
    registerCommandWithTelemetry(reporter, generateClientCommand.getName(), async () => await generateClientCommand.execute()),

    vscode.workspace.onDidChangeWorkspaceFolders(async () => {
      const generatedOutput = context.workspaceState.get<GeneratedOutputState>('generatedOutput');
      if (generatedOutput) {
        await displayGenerationResults(openApiTreeProvider, getGenerationConfiguration());
        // Clear the state 
        void context.workspaceState.update('generatedOutput', undefined);
      }
    }),
    registerCommandWithTelemetry(reporter, searchOrOpenApiDescriptionCommand.getName(),
      async (searchParams: Partial<IntegrationParams> = {}) => await searchOrOpenApiDescriptionCommand.execute(searchParams)
    ),
    registerCommandWithTelemetry(reporter, closeDescriptionCommand.getName(), async () => await closeDescriptionCommand.execute()),
    registerCommandWithTelemetry(reporter, filterDescriptionCommand.getName(), async () => await filterDescriptionCommand.execute()),
    registerCommandWithTelemetry(reporter, editPathsCommand.getName(), async (clientKey: string, clientObject: ClientOrPluginProperties, generationType: string) => {
      clientOrPluginKey = clientKey;
      clientOrPluginObject = clientObject;
      setWorkspaceGenerationType(generationType);
      await editPathsCommand.execute({ clientKey, clientObject });
    }),

    registerCommandWithTelemetry(reporter, `${treeViewId}.regenerateButton`, async () => {
      const configuration = getGenerationConfiguration();
      const regenerate = await confirmOverride();
      if (!regenerate) {
        return;
      }

      if (!clientOrPluginKey || clientOrPluginKey === '') {
        clientOrPluginKey = configuration.clientClassName || configuration.pluginName || '';
      }

      if (!configuration) {
        setGenerationConfiguration({
          outputPath: clientOrPluginObject.outputPath,
          clientClassName: clientOrPluginKey,
        });
      }

      const settings = getExtensionSettings(extensionId);
      const selectedPaths = openApiTreeProvider.getSelectedPaths();
      if (selectedPaths.length === 0) {
        await vscode.window.showErrorMessage(
          vscode.l10n.t("No endpoints selected, select endpoints first")
        );
        return;
      }
      const workspaceGenerationType = getWorkspaceGenerationType();
      const configObject = clientOrPluginObject || configuration;

      if (isClientType(workspaceGenerationType)) {
        await regenerateClient(clientOrPluginKey, configObject, settings, selectedPaths);
      }
      if (isPluginType(workspaceGenerationType)) {
        await regeneratePlugin(clientOrPluginKey, configObject, settings, selectedPaths);
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
      const existingApiManifestFileUris = await vscode.workspace.findFiles(`**/${API_MANIFEST_FILE}`);
      if (existingApiManifestFileUris.length > 0) {
        await Promise.all(existingApiManifestFileUris.map(uri => showUpgradeWarningMessage(uri, null, null, context)));
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

export function registerCommandWithTelemetry(reporter: TelemetryReporter, command: string, callback: (...args: any[]) => any, thisArg?: any): vscode.Disposable {
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

// This method is called when your extension is deactivated
export function deactivate() { }
