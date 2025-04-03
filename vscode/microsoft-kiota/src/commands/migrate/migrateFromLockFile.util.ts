import { KiotaLogEntry, LogLevel, migrateFromLockFile } from "@microsoft/kiota";
import * as vscode from "vscode";

import { KIOTA_LOCK_FILE } from "../../constants";
import { getWorkspaceJsonPath } from "../../util";

export async function checkForLockFileAndPrompt(context: vscode.ExtensionContext) {
    const workspaceFolders = vscode.workspace.workspaceFolders;

    if (workspaceFolders) {
        const lockFile = await vscode.workspace.findFiles(`{**/${KIOTA_LOCK_FILE},${KIOTA_LOCK_FILE}}`);

        if (lockFile.length > 0) {
            const result = await vscode.window.showInformationMessage(
                vscode.l10n.t("Please migrate your API clients to Kiota workspace."),
                vscode.l10n.t("OK"),
                vscode.l10n.t("Remind me later")
            );

            if (result === vscode.l10n.t("OK")) {
                await handleMigration(workspaceFolders![0]);
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
            vscode.window.showInformationMessage(vscode.l10n.t("API clients migrated successfully!"));
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


export async function handleMigration(workspaceFolder: vscode.WorkspaceFolder): Promise<void> {
    vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        title: vscode.l10n.t("Migrating your API clients..."),
        cancellable: false
    }, async (progress) => {
        progress.report({ increment: 0 });

        try {
            const migrationResult = await migrateFromLockFile(workspaceFolder.uri.fsPath);

            progress.report({ increment: 100 });

            if ((migrationResult?.length ?? 0) > 0) {
                displayMigrationMessages(migrationResult!);
            } else {
                vscode.window.showWarningMessage(vscode.l10n.t("Migration completed, but no changes were detected."));
            }
        } catch (error) {
            vscode.window.showErrorMessage(vscode.l10n.t(`Migration failed: ${error}`));
        }
    });
}