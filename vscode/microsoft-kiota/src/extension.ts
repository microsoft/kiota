// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from "vscode";
import * as cp from 'child_process';
import * as rpc from 'vscode-jsonrpc/node';

let kiotaStatusBarItem: vscode.StatusBarItem;
let kiotaOutputChannel: vscode.LogOutputChannel;

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export async function activate(context: vscode.ExtensionContext) : Promise<void> {
  kiotaOutputChannel = vscode.window.createOutputChannel("Kiota", { log: true });
  kiotaOutputChannel.debug
  const statusBarCommandId = "microsoft-kiota.status";
  context.subscriptions.push(
    vscode.commands.registerCommand(statusBarCommandId, async () => {
      const response = await vscode.window.showInformationMessage(
        `Open installation instructions for kiota?`,
        "Yes",
        "No"
      );
      if (response === "Yes") {
        vscode.env.openExternal(
          vscode.Uri.parse(
            "https://aka.ms/get/kiota"
          )
        );
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
      if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
        vscode.window.showErrorMessage("No workspace folder found, open a folder first");
        return;
      }
      await updateStatusBarItem();
      try {
        kiotaOutputChannel.clear();
        kiotaOutputChannel.show();
        kiotaOutputChannel.info(`updating workspace with path ${vscode.workspace.workspaceFolders[0].uri.fsPath}`);
        const result = await runKiotaCommand(["update", "-o", vscode.workspace.workspaceFolders[0].uri.fsPath]);
        if(result.stderr) {
          kiotaOutputChannel.error(result.stderr);
          vscode.window.showErrorMessage(result.stderr);
        } else {
          kiotaOutputChannel.info(result.stdout);
          vscode.window.showInformationMessage(result.stdout);
        }
      } catch (error) {
        const result = error as KiotaCommandResult;
        if(result.stderr) {
          kiotaOutputChannel.error(result.stderr);
          vscode.window.showErrorMessage(result.stderr);
        } else if (result.stdout) {
          const cleanedUpOutput = result
                                    .stdout
                                    .replace(/\sKiota.Builder.KiotaBuilder\[0\](\r\n|\n|\r|\s)+/gm, "")
                                    .split("\r\n").filter((line) => line.startsWith("erro:") || line.startsWith("crit:"))
                                    .join("\r\n");
          kiotaOutputChannel.error(cleanedUpOutput);
          vscode.window.showErrorMessage(cleanedUpOutput);
          //this is janky the console app should write to stderr, or we should use wasm and implement a logger
        }
      }
    }
  );

  context.subscriptions.push(disposable);
}

type KiotaCommandResult = { stdout: string; stderr: string };

function runKiotaCommand(args: string[]): Promise<KiotaCommandResult> {
  return new Promise((resolve, reject) => {
    const child = cp.spawn("kiota", args);
    let stdout = "";
    let stderr = "";
    child.stdout.on("data", (data: string) => (stdout += data));
    child.stderr.on("data", (data: string) => (stderr += data));

    child.on('close', (code: number) => {
      if (code !== 0) {
        reject({ stdout, stderr });
      } else {
        resolve({ stdout, stderr });
      }
    });
  });
}

async function getKiotaVersion(): Promise<string> {
  const childProcess = cp.spawn("C:\\sources\\github\\kiota\\src\\Kiota.JsonRpcServer\\bin\\Debug\\net7.0\\Kiota.JsonRpcServer.exe", ["stdio"]);
  let connection = rpc.createMessageConnection(
    new rpc.StreamMessageReader(childProcess.stdout),
    new rpc.StreamMessageWriter(childProcess.stdin));
  
  let request = new rpc.RequestType0<string, void>('GetVersion');
  
  connection.listen();
  
  const result = await connection.sendRequest(request);
  connection.dispose();
  childProcess.kill();
  if (result) {
    const version = result.split("+")[0];
    if (version) {
      kiotaOutputChannel.info(`kiota version: ${version}`);
      return version;
    }
  }
  kiotaOutputChannel.error(`kiota version: not found`);
  kiotaOutputChannel.show();
  return '';
}

async function updateStatusBarItem(): Promise<void> {
  try {
    const version = await getKiotaVersion();
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
