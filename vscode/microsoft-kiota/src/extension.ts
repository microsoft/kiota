// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import TelemetryReporter from '@vscode/extension-telemetry';

import * as path from 'path';
import * as vscode from "vscode";

import { CodeLensProvider } from "./codelensProvider";
import { KiotaStatusCommand } from "./commands/KiotaStatusCommand";
import { OpenApiTreeNodeCommand } from "./commands/OpenApiTreeNodeCommand";

import { CloseDescriptionCommand } from './commands/CloseDescriptionCommand';
import { EditPathsCommand } from './commands/EditPathsCommand';
import { FilterDescriptionCommand } from './commands/FilterDescriptionCommand';
import { GenerateCommand } from './commands/GenerateCommand';
import { GeneratedOutputState } from './commands/GeneratedOutputState';
import { RegenerateButtonCommand } from './commands/regenerate/RegenerateButtonCommand';
import { RegenerateCommand } from './commands/regenerate/RegenerateCommand';
import { SearchOrOpenApiDescriptionCommand } from './commands/SearchOrOpenApiDescriptionCommand';
import { KIOTA_WORKSPACE_FILE, dependenciesInfo, extensionId, statusBarCommandId, treeViewId } from "./constants";
import { DependenciesViewProvider } from "./dependenciesViewProvider";
import { getExtensionSettings } from "./extensionSettings";
import { getKiotaVersion } from "./getKiotaVersion";
import {
  ClientOrPluginProperties,
  KiotaLogEntry
} from "./kiotaInterop";
import { OpenApiTreeNode, OpenApiTreeProvider } from "./openApiTreeProvider";
import { GenerateState } from "./steps";
import { updateClients } from "./updateClients";
import { loadLockFile, openTreeViewWithProgress } from './utilities/file';
import { exportLogsAndShowErrors, kiotaOutputChannel } from './utilities/logging';
import { showUpgradeWarningMessage } from './utilities/messaging';
import { loadTreeView } from "./workspaceTreeProvider";

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
  const editPathsCommand = new EditPathsCommand(openApiTreeProvider, clientOrPluginKey, clientOrPluginObject);
  const regenerateButtonCommand = new RegenerateButtonCommand(context, openApiTreeProvider, clientOrPluginKey, clientOrPluginObject, workspaceGenerationType);
  const regenerateCommand = new RegenerateCommand(context, openApiTreeProvider, clientOrPluginKey, clientOrPluginObject, workspaceGenerationType);

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
    registerCommandWithTelemetry(reporter, `${treeViewId}.generateClient`, () => generateCommand.execute()),
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
    registerCommandWithTelemetry(reporter, `${extensionId}.editPaths`, async () => editPathsCommand.execute()),
    registerCommandWithTelemetry(reporter, `${treeViewId}.regenerateButton`, async () => regenerateButtonCommand.execute(config)),
    registerCommandWithTelemetry(reporter, `${extensionId}.regenerate`, async () => regenerateCommand.execute()),
  );

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
