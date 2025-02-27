import * as vscode from "vscode";
import { ExtensionContext, Uri, workspace } from "vscode";

import { extensionId } from "../../constants";
import { Command } from "../Command";
import { handleMigration } from "./migrateFromLockFile.util";

export class MigrateFromLockFileCommand extends Command {
  private _context: ExtensionContext;

  constructor(context: ExtensionContext) {
    super();
    this._context = context;
  }

  public getName(): string {
    return `${extensionId}.migrateFromLockFile`;
  }

  public async execute(uri: Uri): Promise<void> {
    const workspaceFolder = workspace.getWorkspaceFolder(uri);

    if (!workspaceFolder) {
      vscode.window.showErrorMessage(vscode.l10n.t("Could not determine the workspace folder."));
      return;
    }

    await handleMigration(workspaceFolder);
  }
}