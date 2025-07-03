import { ClientOrPluginProperties } from "@microsoft/kiota";
import * as vscode from 'vscode';

import { extensionId, SHOW_MESSAGE_AFTER_API_LOAD, treeViewId } from "../constants";
import { OpenApiTreeProvider } from "../providers/openApiTreeProvider";
import { WorkspaceGenerationContext } from "../types/WorkspaceGenerationContext";
import { updateTreeViewIcons } from "../util";
import { openTreeViewWithProgress } from "../utilities/progress";
import { Command } from "./Command";

export class EditPathsCommand extends Command {

  constructor(
    private openApiTreeProvider: OpenApiTreeProvider,
    private context: vscode.ExtensionContext
  ) {
    super();
  }

  public getName(): string {
    return `${extensionId}.editPaths`;
  }

  public async execute({ clientOrPluginKey, clientOrPluginObject }: Partial<WorkspaceGenerationContext>): Promise<void> {
    await this.loadEditPaths(clientOrPluginKey!, clientOrPluginObject!);
    this.openApiTreeProvider.resetInitialState();
  }

  private async loadEditPaths(clientOrPluginKey: string, clientOrPluginObject: ClientOrPluginProperties) {
    await openTreeViewWithProgress(
      async () => {
        await this.openApiTreeProvider.loadEditPaths(clientOrPluginKey, clientOrPluginObject);
        await updateTreeViewIcons(treeViewId, false, true);
        await vscode.commands.executeCommand('kiota.workspace.refresh');
      }
    );

    const regenerateAnswer = vscode.l10n.t("Regenerate");
    const showGenerateMessage = this.context.globalState.get<boolean>(SHOW_MESSAGE_AFTER_API_LOAD, true);

    if (showGenerateMessage) {
      const doNotShowAgainOption = vscode.l10n.t("Do not show this again");
      const response = await vscode.window.showInformationMessage(
        vscode.l10n.t('Click on Regenerate after selecting the paths in the API Explorer'),
        regenerateAnswer,
        doNotShowAgainOption
      );
      if (response === regenerateAnswer) {
        await vscode.commands.executeCommand(`kiota.regenerate`);
      } else if (response === doNotShowAgainOption) {
        await this.context.globalState.update(SHOW_MESSAGE_AFTER_API_LOAD, false);
      }
    }
  }
}
