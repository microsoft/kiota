// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from "vscode";

let kiotaStatusBarItem: vscode.StatusBarItem;

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export async function activate(context: vscode.ExtensionContext) : Promise<void> {
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

  // register some listener that make sure the status bar
  // item always up-to-date
  context.subscriptions.push(
    vscode.window.onDidChangeActiveTextEditor(async() => await updateStatusBarItem())
  );

  // update status bar item once at start
  await updateStatusBarItem();
  // Use the console to output diagnostic information (console.log) and errors (console.error)
  // This line of code will only be executed once when your extension is activated
  console.log(
    'Congratulations, your extension "microsoft-kiota" is now active!'
  );

  // The command has been defined in the package.json file
  // Now provide the implementation of the command with registerCommand
  // The commandId parameter must match the command field in package.json
  let disposable = vscode.commands.registerCommand(
    "microsoft-kiota.helloWorld",
    () => {
      // The code you place here will be executed every time your command is executed
      // Display a message box to the user
      vscode.window.showInformationMessage("Hello World from Microsoft Kiota!");
    }
  );

  context.subscriptions.push(disposable);
}

function getKiotaVersion(): Promise<string> {
  return new Promise((resolve, reject) => {
    const cp = require("child_process");
    cp.exec("kiota --version", (err: any, stdout: string, stderr: any) => {
      if (stdout) {
        const version = stdout.split("+")[0];
        if (version) {
          resolve(version);
          return;
        }
      }
      reject();
    });
  });
}

async function updateStatusBarItem(): Promise<void> {
  try {
    const version = await getKiotaVersion();
    kiotaStatusBarItem.text = `$(extensions-info-message) kiota ${version}`;
    kiotaStatusBarItem.show();
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
