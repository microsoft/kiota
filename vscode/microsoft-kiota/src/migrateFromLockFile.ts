import { connectToKiota, KiotaLogEntry, LogLevel } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

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

export function displayMigrationMessages(logEntries: KiotaLogEntry[]) {
    const successEntries = logEntries.filter(entry => 
        entry.level === LogLevel.information && entry.message.includes("migrated successfully")
    );

    if (successEntries.length > 0) {
        successEntries.forEach(entry => {
            vscode.window.showInformationMessage(vscode.l10n.t("Api clients migrated successfully!"));
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