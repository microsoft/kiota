// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from "vscode";
import { OpenApiTreeNode, OpenApiTreeProvider } from "./openApiTreeProvider";
import {
  getLogEntriesForLevel,
  KiotaGenerationLanguage,
  KiotaLogEntry,
  LogLevel,
  parseGenerationLanguage,
} from "./kiotaInterop";
import { generateSteps, openSteps, searchSteps } from "./steps";
import { getKiotaVersion } from "./getKiotaVersion";
import { searchDescription } from "./searchDescription";
import { generateClient } from "./generateClient";
import { getLanguageInformation } from "./getLanguageInformation";
import { DependenciesViewProvider } from "./dependenciesViewProvider";
import { updateClients } from "./updateClients";

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
  const extensionId = "kiota";
  const focusCommandId = ".focus";
  const statusBarCommandId = `${extensionId}.status`;
  const treeViewId = `${extensionId}.openApiExplorer`;
  const dependenciesInfo = `${extensionId}.dependenciesInfo`;
  const openApiTreeProvider = new OpenApiTreeProvider(context);
  const dependenciesInfoProvider = new DependenciesViewProvider(
    context.extensionUri
  );
  context.subscriptions.push(
    vscode.commands.registerCommand(
      `${extensionId}.selectLock`,
      async (node: { fsPath: string }) => {
        await vscode.window.withProgress({
          location: vscode.ProgressLocation.Notification,
          cancellable: false,
          title: vscode.l10n.t("Loading...")
        }, (progress, _) => openApiTreeProvider.loadLockFile(node.fsPath));
        if (openApiTreeProvider.descriptionUrl) {
          vscode.commands.executeCommand(`${treeViewId}${focusCommandId}`);
        }
      }
    ),
    vscode.commands.registerCommand(statusBarCommandId, async () => {
      const yesAnswer = vscode.l10n.t("Yes");
      const response = await vscode.window.showInformationMessage(
        vscode.l10n.t("Open installation instructions for kiota?"),
        yesAnswer,
        vscode.l10n.t("No")
      );
      if (response === yesAnswer) {
        vscode.env.openExternal(vscode.Uri.parse("https://aka.ms/get/kiota"));
      }
    }),
    vscode.window.registerWebviewViewProvider(
      dependenciesInfo,
      dependenciesInfoProvider
    ),
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
            vscode.l10n.t("No endpoints selected, select endpoints first")
          );
          return;
        }
        if (
          !vscode.workspace.workspaceFolders ||
          vscode.workspace.workspaceFolders.length === 0
        ) {
          vscode.window.showErrorMessage(
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
        if (!openApiTreeProvider.descriptionUrl) {
          vscode.window.showErrorMessage(
            vscode.l10n.t("No description found, select a description first")
          );
          return;
        }
        const language =
          typeof config.language === "string"
            ? parseGenerationLanguage(config.language)
            : KiotaGenerationLanguage.CSharp;
        const result = await vscode.window.withProgress({
          location: vscode.ProgressLocation.Notification,
          cancellable: false,
          title: vscode.l10n.t("Generating client...")
        }, (progress, _) => {
          return generateClient(
            context,
            openApiTreeProvider.descriptionUrl,
            typeof config.outputPath === "string"
              ? config.outputPath
              : "./output",
            language,
            selectedPaths,
            [],
            typeof config.clientClassName === "string"
              ? config.clientClassName
              : "ApiClient",
            typeof config.clientNamespaceName === "string"
              ? config.clientNamespaceName
              : "ApiSdk"
          );
        });

        if (result)
        {
          exportLogsAndShowErrors(result);
        }
        languagesInformation = await getLanguageInformation(
          context,
          language,
          openApiTreeProvider.descriptionUrl
        );
        if (languagesInformation) {
          dependenciesInfoProvider.update(languagesInformation, language);
          vscode.commands.executeCommand(`${dependenciesInfo}${focusCommandId}`);
        }
      }
    ),
    vscode.commands.registerCommand(
      `${extensionId}.searchApiDescription`,
      async () => {
        const config = await searchSteps(x => searchDescription(context, x));
        if (config.descriptionPath) {
          openApiTreeProvider.descriptionUrl = config.descriptionPath;
          vscode.commands.executeCommand(`${treeViewId}${focusCommandId}`);
        }
      }
    ),
    vscode.commands.registerCommand(`${treeViewId}.closeDescription`, () =>
      openApiTreeProvider.closeDescription()
    ),
    vscode.commands.registerCommand(
      `${treeViewId}.openDescription`,
      async () => {
        const openState = await openSteps();
        if (openState.descriptionPath) {
          openApiTreeProvider.descriptionUrl = openState.descriptionPath;
          vscode.commands.executeCommand(`${treeViewId}${focusCommandId}`);
        }
      }
    )
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
        vscode.window.showErrorMessage(
          vscode.l10n.t("No workspace folder found, open a folder first")
        );
        return;
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
          return updateClients(context);
        });
        if (res) {
          exportLogsAndShowErrors(res);
        }
      } catch (error) {
        kiotaOutputChannel.error(
          vscode.l10n.t("error updating the clients {error}"),
          error
        );
        vscode.window.showErrorMessage(
          vscode.l10n.t("error updating the clients {error}"),
          error as string
        );
      }
    }
  );

  context.subscriptions.push(disposable);
}

function exportLogsAndShowErrors(result: KiotaLogEntry[]) : void {
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
    errorMessages.forEach((element) => {
      vscode.window.showErrorMessage(element.message);
    });
  } else {
    informationMessages.forEach((element) => {
      vscode.window.showInformationMessage(element.message);
    });
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

// This method is called when your extension is deactivated
export function deactivate() {}
