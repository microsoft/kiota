// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import { ClientOrPluginProperties, setKiotaConfig } from "@microsoft/kiota";
import { TelemetryReporter } from '@vscode/extension-telemetry';
import * as vscode from "vscode";

import { CloseDescriptionCommand } from './commands/closeDescriptionCommand';
import { DeleteWorkspaceItemCommand } from './commands/deleteWorkspaceItem/deleteWorkspaceItemCommand';
import { EditPathsCommand } from './commands/editPathsCommand';
import { GenerateClientCommand } from './commands/generate/generateClientCommand';
import { displayGenerationResults } from './commands/generate/generation-util';
import { checkForLockFileAndPrompt } from "./commands/migrate/migrateFromLockFile.util";
import { MigrateFromLockFileCommand } from './commands/migrate/migrateFromLockFileCommand';
import { SearchOrOpenApiDescriptionCommand } from './commands/openApidescription/searchOrOpenApiDescriptionCommand';
import { AddAllToSelectedEndpointsCommand } from './commands/openApiTreeView/addAllToSelectedEndpointsCommand';
import { AddToSelectedEndpointsCommand } from './commands/openApiTreeView/addToSelectedEndpointsCommand';
import { FilterDescriptionCommand } from './commands/openApiTreeView/filterDescriptionCommand';
import { OpenDocumentationPageCommand } from './commands/openApiTreeView/openDocumentationPageCommand';
import { RemoveAllFromSelectedEndpointsCommand } from './commands/openApiTreeView/removeAllFromSelectedEndpointsCommand';
import { RemoveFromSelectedEndpointsCommand } from './commands/openApiTreeView/removeFromSelectedEndpointsCommand';
import { RegenerateButtonCommand } from './commands/regenerate/regenerateButtonCommand';
import { RegenerateCommand } from './commands/regenerate/regenerateCommand';
import { SelectLockCommand } from './commands/selectLockCommand';
import { StatusCommand } from './commands/statusCommand';
import { UpdateClientsCommand } from './commands/updateClients/updateClientsCommand';
import { dependenciesInfo, extensionId, statusBarCommandId, treeViewId } from "./constants";
import { getGenerationConfiguration } from './handlers/configurationHandler';
import { UriHandler } from './handlers/uriHandler';
import { WorkspaceContentService } from './modules/workspace';
import { CodeLensProvider } from './providers/codelensProvider';
import { DependenciesViewProvider } from "./providers/dependenciesViewProvider";
import { OpenApiTreeNode, OpenApiTreeProvider } from "./providers/openApiTreeProvider";
import { SharedService } from './providers/sharedService';
import { loadTreeView, WorkspaceTreeItem, WorkspaceTreeProvider } from './providers/workspaceTreeProvider';
import { getExtensionSettings } from "./types/extensionSettings";
import { GeneratedOutputState } from './types/GeneratedOutputState';
import { WorkspaceGenerationContext } from "./types/WorkspaceGenerationContext";
import { IntegrationParams } from './utilities/deep-linking';
import { loadWorkspaceFile } from './utilities/file';
import { updateStatusBarItem } from './utilities/status';

let kiotaStatusBarItem: vscode.StatusBarItem;
let kiotaOutputChannel: vscode.LogOutputChannel;
let workspaceGenerationContext: WorkspaceGenerationContext;

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export async function activate(
  context: vscode.ExtensionContext
): Promise<void> {
  setKiotaConfig({ binaryLocation: context.asAbsolutePath('') });
  kiotaOutputChannel = vscode.window.createOutputChannel("Kiota", {
    log: true,
  });
  const sharedService = SharedService.getInstance();
  const workspaceContentService = new WorkspaceContentService();
  const openApiTreeProvider = new OpenApiTreeProvider(context, () => getExtensionSettings(extensionId), sharedService);
  const dependenciesInfoProvider = new DependenciesViewProvider(
    context.extensionUri
  );
  const reporter = new TelemetryReporter(context.extension.packageJSON.telemetryInstrumentationKey);
  const workspaceTreeProvider = new WorkspaceTreeProvider(workspaceContentService, sharedService);

  const setWorkspaceGenerationContext = (params: Partial<WorkspaceGenerationContext>) => {
    workspaceGenerationContext = { ...workspaceGenerationContext, ...params };
  };

  const uriHandler = new UriHandler(context, openApiTreeProvider);
  const migrateFromLockFileCommand = new MigrateFromLockFileCommand(context);
  const addAllToSelectedEndpointsCommand = new AddAllToSelectedEndpointsCommand(openApiTreeProvider);
  const addToSelectedEndpointsCommand = new AddToSelectedEndpointsCommand(openApiTreeProvider);
  const removeAllFromSelectedEndpointsCommand = new RemoveAllFromSelectedEndpointsCommand(openApiTreeProvider);
  const removeFromSelectedEndpointsCommand = new RemoveFromSelectedEndpointsCommand(openApiTreeProvider);
  const filterDescriptionCommand = new FilterDescriptionCommand(openApiTreeProvider);
  const openDocumentationPageCommand = new OpenDocumentationPageCommand();
  const editPathsCommand = new EditPathsCommand(openApiTreeProvider, context);
  const searchOrOpenApiDescriptionCommand = new SearchOrOpenApiDescriptionCommand(openApiTreeProvider, context);
  const generateClientCommand = new GenerateClientCommand(openApiTreeProvider, context, dependenciesInfoProvider, setWorkspaceGenerationContext, kiotaOutputChannel);
  const regenerateCommand = new RegenerateCommand(context, openApiTreeProvider, kiotaOutputChannel);
  const regenerateButtonCommand = new RegenerateButtonCommand(context, openApiTreeProvider, kiotaOutputChannel);
  const closeDescriptionCommand = new CloseDescriptionCommand(openApiTreeProvider);
  const statusCommand = new StatusCommand();
  const selectLockCommand = new SelectLockCommand(openApiTreeProvider);
  const deleteWorkspaceItemCommand = new DeleteWorkspaceItemCommand(context, openApiTreeProvider, kiotaOutputChannel, sharedService);
  const updateClientsCommand = new UpdateClientsCommand(context, kiotaOutputChannel);

  await loadTreeView(context, workspaceTreeProvider);
  await checkForLockFileAndPrompt(context);
  let codeLensProvider = new CodeLensProvider();
  context.subscriptions.push(
    vscode.window.registerUriHandler({ handleUri: async (uri: vscode.Uri) => await uriHandler.handleUri(uri) }),
    vscode.languages.registerCodeLensProvider('json', codeLensProvider),
    reporter,
    registerCommandWithTelemetry(reporter, selectLockCommand.getName(), async (x) => await loadWorkspaceFile(x, openApiTreeProvider)),
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
    registerCommandWithTelemetry(reporter, editPathsCommand.getName(), async (clientOrPluginKey: string, clientOrPluginObject: ClientOrPluginProperties, generationType: string) => {
      setWorkspaceGenerationContext({ clientOrPluginKey, clientOrPluginObject, generationType });
      await editPathsCommand.execute({ clientOrPluginKey, clientOrPluginObject, generationType });
    }),
    registerCommandWithTelemetry(reporter, regenerateButtonCommand.getName(), async () => {
      await regenerateButtonCommand.execute(workspaceGenerationContext);
    }),
    registerCommandWithTelemetry(reporter, regenerateCommand.getName(), async (clientOrPluginKey: string, clientOrPluginObject: ClientOrPluginProperties, generationType: string) => {
      setWorkspaceGenerationContext({ clientOrPluginKey, clientOrPluginObject, generationType });
      await regenerateCommand.execute({ clientOrPluginKey, clientOrPluginObject, generationType });
    }),
    registerCommandWithTelemetry(reporter, migrateFromLockFileCommand.getName(), async (uri: vscode.Uri) => await migrateFromLockFileCommand.execute(uri)),
    registerCommandWithTelemetry(reporter, deleteWorkspaceItemCommand.getName(), async (workspaceTreeItem: WorkspaceTreeItem) => await deleteWorkspaceItemCommand.execute(workspaceTreeItem)),

  );

  // create a new status bar item that we can now manage
  kiotaStatusBarItem = vscode.window.createStatusBarItem(
    vscode.StatusBarAlignment.Right,
    100
  );
  kiotaStatusBarItem.command = statusBarCommandId;
  context.subscriptions.push(kiotaStatusBarItem);

  // update status bar item once at start
  await updateStatusBarItem(kiotaOutputChannel, kiotaStatusBarItem);
  context.subscriptions.push(vscode.commands.registerCommand(updateClientsCommand.getName(), async () => await updateClientsCommand.execute({ kiotaStatusBarItem })));
}

function registerCommandWithTelemetry(reporter: TelemetryReporter, command: string, callback: (...args: any[]) => any, thisArg?: any): vscode.Disposable {
  return vscode.commands.registerCommand(command, (...args: any[]) => {
    const splatCommand = command.split('/');
    const eventName = splatCommand[splatCommand.length - 1];
    reporter.sendTelemetryEvent(eventName);
    return callback.apply(thisArg, args);
  }, thisArg);
}

// This method is called when your extension is deactivated
export function deactivate() { }
