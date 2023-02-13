// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from "vscode";

let kiotaStatusBarItem: vscode.StatusBarItem;

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {
  const statusBarCommandId = "microsoft-kiota.status";
  context.subscriptions.push(
    vscode.commands.registerCommand(statusBarCommandId, () => {
      // const n = getNumberOfSelectedLines(vscode.window.activeTextEditor);
      vscode.window.showInformationMessage(
        `Yeah, 10 line(s) selected... Keep going!`
      );
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
    vscode.window.onDidChangeActiveTextEditor(updateStatusBarItem)
  );

  // update status bar item once at start
  updateStatusBarItem();
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

function updateStatusBarItem(): void {
  const cp = require("child_process");
  cp.exec("kiota2 --version", (err: any, stdout: string, stderr: any) => {
    if (stdout) {
      const version = stdout.split("+")[0];
      if (version) {
        kiotaStatusBarItem.text = `$(extensions-info-message) kiota ${version}`;
        kiotaStatusBarItem.show();
        return;
      }
    }
    kiotaStatusBarItem.text = `$(extensions-warning-message) kiota not found`;
    kiotaStatusBarItem.backgroundColor = new vscode.ThemeColor(
      "statusBarItem.errorBackground"
    );
    kiotaStatusBarItem.show();
  });
}

// This method is called when your extension is deactivated
export function deactivate() {}
