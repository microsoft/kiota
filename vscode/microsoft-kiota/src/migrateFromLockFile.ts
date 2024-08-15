import { connectToKiota, KiotaLogEntry, LogLevel } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";
import { KIOTA_LOCK_FILE } from "./constants";
import { getWorkspaceJsonPath, handleMigration } from "./util";

export function migrateFromLockFile(context: vscode.ExtensionContext, lockFileDirectory: string): Promise<KiotaLogEntry[] | undefined> {
    return connectToKiota(context, async (connection) => {
        const request = new rpc.RequestType1<string, KiotaLogEntry[], void>(
            "MigrateFromLockFile"
        );
        const result = await connection.sendRequest(
            request,
            lockFileDirectory
        );
        return result;
    });
};

export async function checkForLockFileAndPrompt(context: vscode.ExtensionContext) {
    const workspaceFolders = vscode.workspace.workspaceFolders;

    if(workspaceFolders) {
      const lockFile = await vscode.workspace.findFiles(`{**/${KIOTA_LOCK_FILE},${KIOTA_LOCK_FILE}}`);

      if (lockFile.length > 0) {
        const result = await vscode.window.showInformationMessage(
          vscode.l10n.t("Please migrate your API clients to Kiota workspace."),
          vscode.l10n.t("OK"),
          vscode.l10n.t("Remind me later")
        );

        if (result === vscode.l10n.t("OK")) {
          await handleMigration(context, workspaceFolders![0]);
          await vscode.commands.executeCommand('kiota.workspace.refresh');
        }  
      }
    }
  };

export function displayMigrationMessages(logEntries: KiotaLogEntry[]) {
    const workspaceJsonUri = vscode.Uri.file(getWorkspaceJsonPath());
    const successEntries = logEntries.filter(entry => 
        entry.level === LogLevel.information && entry.message.includes("migrated successfully")
    );

    if (successEntries.length > 0) {
        successEntries.forEach(entry => {
            vscode.window.showInformationMessage(vscode.l10n.t("Api clients migrated successfully!"));
            vscode.commands.executeCommand('kiota.workspace.refresh');
            vscode.commands.executeCommand('kiota.workspace.openWorkspaceFile', workspaceJsonUri);
        });
    } else {
        logEntries.forEach(entry => {
            switch (entry.level) {
                case LogLevel.warning:
                    vscode.window.showWarningMessage(vscode.l10n.t(entry.message));
                    break;
                case LogLevel.error:
                case LogLevel.critical:
                    vscode.window.showErrorMessage(vscode.l10n.t(entry.message));
                    break;
                default:
                    break;
            }
        });
    }
}