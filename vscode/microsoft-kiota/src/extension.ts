// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from "vscode";
import TelemetryReporter from '@vscode/extension-telemetry';
import * as path from 'path';
import * as fs from 'fs';
import { OpenApiTreeNode, OpenApiTreeProvider } from "./openApiTreeProvider";
import {
  generationLanguageToString,
  getLogEntriesForLevel,
  KiotaGenerationLanguage,
  KiotaLogEntry,
  LogLevel,
  parseGenerationLanguage,
} from "./kiotaInterop";
import { filterSteps, generateSteps, openSteps, searchLockSteps, searchSteps } from "./steps";
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
  const reporter = new TelemetryReporter(context.extension.packageJSON.telemetryInstrumentationKey);
  context.subscriptions.push(
    reporter,
    registerCommandWithTelemetry(reporter, 
      `${extensionId}.searchLock`,
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
              : "ApiSdk"
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
    registerCommandWithTelemetry(reporter, 
      `${extensionId}.searchApiDescription`,
      async () => {
        const config = await searchSteps(x => vscode.window.withProgress({
          location: vscode.ProgressLocation.Notification,
          cancellable: false,
          title: vscode.l10n.t("Searching...")
        }, (progress, _) => {
          return searchDescription(context, x);
        }));
        if (config.descriptionPath) {
          await openApiTreeProvider.setDescriptionUrl(config.descriptionPath);
          await vscode.commands.executeCommand(`${treeViewId}${focusCommandId}`);
        }
      }
    ),
    registerCommandWithTelemetry(reporter, `${treeViewId}.closeDescription`, () =>
      openApiTreeProvider.closeDescription()
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
          await openApiTreeProvider.setDescriptionUrl(openState.descriptionPath);
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
