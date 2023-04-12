// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from "vscode";
import * as path from 'path';
import { OpenApiTreeNode, OpenApiTreeProvider } from "./openApiTreeProvider";
import {
  getLogEntriesForLevel,
  KiotaGenerationLanguage,
  KiotaLogEntry,
  LogLevel,
  parseGenerationLanguage,
} from "./kiotaInterop";
import { generateSteps, openSteps, searchLockSteps, searchSteps } from "./steps";
import { getKiotaVersion } from "./getKiotaVersion";
import { searchDescription } from "./searchDescription";
import { generateClient } from "./generateClient";
import { getLanguageInformation } from "./getLanguageInformation";
import { DependenciesViewProvider } from "./dependenciesViewProvider";
import { updateClients } from "./updateClients";

let kiotaStatusBarItem: vscode.StatusBarItem;
let kiotaOutputChannel: vscode.LogOutputChannel;
const extensionId = "kiota";
const focusCommandId = ".focus";
const statusBarCommandId = `${extensionId}.status`;
const treeViewId = `${extensionId}.openApiExplorer`;
const dependenciesInfo = `${extensionId}.dependenciesInfo`;
export const kiotaLockFile = "kiota-lock.json";

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export async function activate(
  context: vscode.ExtensionContext
): Promise<void> {
  kiotaOutputChannel = vscode.window.createOutputChannel("Kiota", {
    log: true,
  });
  const openApiTreeProvider = new OpenApiTreeProvider(context);
  const dependenciesInfoProvider = new DependenciesViewProvider(
    context.extensionUri
  );
  context.subscriptions.push(
    vscode.commands.registerCommand(
      `${extensionId}.searchLock`,
      async () => {
        const lockFilePath = await searchLockSteps();
        if (lockFilePath && lockFilePath.lockFilePath) {
          await loadLockFile(lockFilePath.lockFilePath, openApiTreeProvider);
        }
      }),
    vscode.commands.registerCommand(
      `${extensionId}.selectLock`,
      (x) => loadLockFile(x, openApiTreeProvider)
    ),
    vscode.commands.registerCommand(statusBarCommandId, async () => {
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
        if (!openApiTreeProvider.descriptionUrl) {
          await vscode.window.showErrorMessage(
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
        
        languagesInformation = await getLanguageInformation(
          context,
          language,
          openApiTreeProvider.descriptionUrl
        );
        if (languagesInformation) {
          dependenciesInfoProvider.update(languagesInformation, language);
          await vscode.commands.executeCommand(`${dependenciesInfo}${focusCommandId}`);
        }
        if (typeof config.outputPath === "string" && !openApiTreeProvider.isLockFileLoaded && 
            vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0 &&
            result && getLogEntriesForLevel(result, LogLevel.critical, LogLevel.error).length === 0) {
          await openApiTreeProvider.loadLockFile(path.join(vscode.workspace.workspaceFolders[0].uri.fsPath, config.outputPath, kiotaLockFile));
        }
        if (result)
        {
          await exportLogsAndShowErrors(result);
        }
      }
    ),
    vscode.commands.registerCommand(
      `${extensionId}.searchApiDescription`,
      async () => {
        const config = await searchSteps(x => searchDescription(context, x));
        if (config.descriptionPath) {
          openApiTreeProvider.descriptionUrl = config.descriptionPath;
          await vscode.commands.executeCommand(`${treeViewId}${focusCommandId}`);
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
          await vscode.commands.executeCommand(`${treeViewId}${focusCommandId}`);
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
        await vscode.window.showErrorMessage(
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

async function loadLockFile(node: { fsPath: string }, openApiTreeProvider: OpenApiTreeProvider): Promise<void> {
  await vscode.window.withProgress({
    location: vscode.ProgressLocation.Notification,
    cancellable: false,
    title: vscode.l10n.t("Loading...")
  }, (progress, _) => openApiTreeProvider.loadLockFile(node.fsPath));
  if (openApiTreeProvider.descriptionUrl) {
    await vscode.commands.executeCommand(`${treeViewId}${focusCommandId}`);
  }
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

// This method is called when your extension is deactivated
export function deactivate() {}
