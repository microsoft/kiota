import * as vscode from "vscode";

import { extensionId, treeViewId } from "../constants";
import { ClientOrPluginProperties } from "../kiotaInterop";
import { OpenApiTreeProvider } from "../providers/openApiTreeProvider";
import { WorkspaceGenerationContext } from "../types/WorkspaceGenerationContext";
import { updateTreeViewIcons } from "../util";
import { openTreeViewWithProgress } from "../utilities/progress";
import { Command } from "./Command";

export class EditPathsCommand extends Command {

  public constructor(private _openApiTreeProvider: OpenApiTreeProvider) {
    super();
  }

  public getName(): string {
    return `${extensionId}.editPaths`;
  }

  public async execute({ clientOrPluginKey, clientOrPluginObject }: Partial<WorkspaceGenerationContext>): Promise<void> {
    await this.loadEditPaths(clientOrPluginKey!, clientOrPluginObject!);
    this._openApiTreeProvider.resetInitialState();
    await updateTreeViewIcons(treeViewId, false, true);
    await vscode.commands.executeCommand('kiota.workspace.refresh');
  }

  private async loadEditPaths(clientOrPluginKey: string, clientOrPluginObject: ClientOrPluginProperties) {
    await openTreeViewWithProgress(() => this._openApiTreeProvider.loadEditPaths(clientOrPluginKey, clientOrPluginObject));
  }
}
