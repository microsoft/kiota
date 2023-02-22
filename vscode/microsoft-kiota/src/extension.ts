// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from "vscode";
import * as rpc from "vscode-jsonrpc/node";
import { OpenApiTreeNode, OpenApiTreeProvider } from "./openApiTreeProvider";
import { connectToKiota, getLogEntriesForLevel, KiotaGenerationLanguage, KiotaLogEntry, KiotaSearchResult, KiotaSearchResultItem, LogLevel, parseGenerationLanguage } from "./kiotaInterop";
import { generateSteps, searchSteps } from "./steps";

let kiotaStatusBarItem: vscode.StatusBarItem;
let kiotaOutputChannel: vscode.LogOutputChannel;

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export async function activate(
  context: vscode.ExtensionContext
): Promise<void> {
  kiotaOutputChannel = vscode.window.createOutputChannel("Kiota", {
    log: true,
  });
  const extensionId = "microsoft-kiota";
  const statusBarCommandId = `${extensionId}.status`;
  const treeViewId = `${extensionId}.openApiExplorer`;
  context.subscriptions.push(
    vscode.commands.registerCommand(statusBarCommandId, async () => {
      const response = await vscode.window.showInformationMessage(
        `Open installation instructions for kiota?`,
        "Yes",
        "No"
      );
      if (response === "Yes") {
        vscode.env.openExternal(vscode.Uri.parse("https://aka.ms/get/kiota"));
      }
    })
  );

  const openApiTreeProvider = new OpenApiTreeProvider();
  context.subscriptions.push(
    vscode.window.registerTreeDataProvider(treeViewId, openApiTreeProvider),
    vscode.commands.registerCommand(
      `${treeViewId}.addToSelectedEndpoints`,
      (x: OpenApiTreeNode) => openApiTreeProvider.select(x, true, false)
    ),
    vscode.commands.registerCommand(
      `${treeViewId}.addAllToSelectedEndpoints`,
      (x: OpenApiTreeNode) => openApiTreeProvider.select(x, true, true)
    ),
    vscode.commands.registerCommand(
      `${treeViewId}.removeFromSelectedEndpoints`,
      (x: OpenApiTreeNode) => openApiTreeProvider.select(x, false, false)
    ),
    vscode.commands.registerCommand(
      `${treeViewId}.removeAllFromSelectedEndpoints`,
      (x: OpenApiTreeNode) => openApiTreeProvider.select(x, false, true)
    ),
    vscode.commands.registerCommand(
      `${treeViewId}.generateClient`,
      async () => {
        const selectedPaths = openApiTreeProvider.getSelectedPaths();
        if (selectedPaths.length === 0) {
          vscode.window.showErrorMessage(
            "No endpoints selected, select endpoints first"
          );
          return;
        }
        if (
          !vscode.workspace.workspaceFolders ||
          vscode.workspace.workspaceFolders.length === 0
        ) {
          vscode.window.showErrorMessage(
            "No workspace folder found, open a folder first"
          );
          return;
        }
        const config = await generateSteps();
        if (!openApiTreeProvider.descriptionUrl) {
          vscode.window.showErrorMessage(
            "No description url found, select a description first"
          );
          return;
        }
        await generateClient(
          openApiTreeProvider.descriptionUrl,
          typeof config.outputPath === "string" ? config.outputPath : './output',
          typeof config.language === "string" ? parseGenerationLanguage(config.language) : KiotaGenerationLanguage.CSharp,
          selectedPaths,
          [],
          typeof config.clientClassName === "string" ? config.clientClassName : 'ApiClient',
          typeof config.clientNamespaceName === "string" ? config.clientNamespaceName : 'ApiSdk');
    }),
    vscode.commands.registerCommand(
      `${extensionId}.searchApiDescription`,
      async () => {
        const config = await searchSteps(searchDescription);
        if(config.descriptionPath) {
          openApiTreeProvider.descriptionUrl = config.descriptionPath;
          vscode.commands.executeCommand(`${treeViewId}.focus`);
        }
    })
  );

  // create a new status bar item that we can now manage
  kiotaStatusBarItem = vscode.window.createStatusBarItem(
    vscode.StatusBarAlignment.Right,
    100
  );
  kiotaStatusBarItem.command = statusBarCommandId;
  context.subscriptions.push(kiotaStatusBarItem);

  // update status bar item once at start
  await updateStatusBarItem();
  let disposable = vscode.commands.registerCommand(
    "microsoft-kiota.updateClients",
    async () => {
      if (
        !vscode.workspace.workspaceFolders ||
        vscode.workspace.workspaceFolders.length === 0
      ) {
        vscode.window.showErrorMessage(
          "No workspace folder found, open a folder first"
        );
        return;
      }
      await updateStatusBarItem();
      try {
        kiotaOutputChannel.clear();
        kiotaOutputChannel.show();
        kiotaOutputChannel.info(
          `updating workspace with path ${vscode.workspace.workspaceFolders[0].uri.fsPath}`
        );
        await connectToKiota(async (connection) => {
          const request = new rpc.RequestType<string, KiotaLogEntry[], void>(
            "Update"
          );
          const result = await connection.sendRequest(
            request,
            vscode.workspace.workspaceFolders![0].uri.fsPath
          );
          const informationMessages = getLogEntriesForLevel(result, LogLevel.information);
          const errorMessages = getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error);
          if (errorMessages.length > 0) {
            errorMessages.forEach((element) => {
              kiotaOutputChannel.error(element.message);
              vscode.window.showErrorMessage(element.message);
            });
          } else {
            informationMessages.forEach((element) => {
              kiotaOutputChannel.info(element.message);
              vscode.window.showInformationMessage(element.message);
            });
          }
        });
      } catch (error) {
        kiotaOutputChannel.error("error updating the clients {0}", error);
        vscode.window.showErrorMessage(
          "error updating the clients {0}",
          error as string
        );
      }
    }
  );

  context.subscriptions.push(disposable);
}

function searchDescription(searchTerm: string): Promise<Record<string, KiotaSearchResultItem> | undefined> {
  return connectToKiota<Record<string, KiotaSearchResultItem>>(async (connection) => {
    const request = new rpc.RequestType<string, KiotaSearchResult, void>(
      "Search"
    );
    const result = await connection.sendRequest(
      request,
      searchTerm
    );
    if(result) {
      return result.results;
    }
    return undefined;
  });
}

function generateClient(descriptionPath: string, output: string, language: KiotaGenerationLanguage, includeFilters: string[], excludeFilters: string[], clientClassName: string, clientNamespaceName: string): Promise<void> {
  return connectToKiota<void>(async (connection) => {
    const request = new rpc.RequestType7<string, string, KiotaGenerationLanguage, string[], string[], string, string, KiotaLogEntry[], void>(
      "Generate"
    );
    const result = await connection.sendRequest(
      request,
      descriptionPath,
      output,
      language,
      includeFilters,
      excludeFilters,
      clientClassName,
      clientNamespaceName
    );
    const informationMessages = getLogEntriesForLevel(result, LogLevel.information);
    const errorMessages = getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error);
    if (errorMessages.length > 0) {
      errorMessages.forEach((element) => {
        kiotaOutputChannel.error(element.message);
        vscode.window.showErrorMessage(element.message);
      });
    } else {
      informationMessages.forEach((element) => {
        kiotaOutputChannel.info(element.message);
        vscode.window.showInformationMessage(element.message);
      });
    }
  });
}

function getKiotaVersion(): Promise<string | undefined> {
  return connectToKiota<string>(async (connection) => {
    const request = new rpc.RequestType0<string, void>("GetVersion");
    const result = await connection.sendRequest(request);
    if (result) {
      const version = result.split("+")[0];
      if (version) {
        kiotaOutputChannel.info(`kiota version: ${version}`);
        return version;
      }
    }
    kiotaOutputChannel.error(`kiota version: not found`);
    kiotaOutputChannel.show();
    return undefined;
  });
}

async function updateStatusBarItem(): Promise<void> {
  try {
    const version = await getKiotaVersion();
    if (!version) {
      throw new Error("kiota not found");
    }
    kiotaStatusBarItem.text = `$(extensions-info-message) kiota ${version}`;
  } catch (error) {
    kiotaStatusBarItem.text = `$(extensions-warning-message) kiota not found`;
    kiotaStatusBarItem.backgroundColor = new vscode.ThemeColor(
      "statusBarItem.errorBackground"
    );
  }
  kiotaStatusBarItem.show();
}

// This method is called when your extension is deactivated
export function deactivate() {}
