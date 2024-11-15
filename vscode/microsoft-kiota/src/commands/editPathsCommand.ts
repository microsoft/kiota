import { extensionId, treeViewId } from "../constants";
import { ClientOrPluginProperties } from "../kiotaInterop";
import { OpenApiTreeProvider } from "../providers/openApiTreeProvider";
import { WorkspaceTreeProvider } from "../providers/workspaceTreeProvider";
import { WorkspaceGenerationContext } from "../types/WorkspaceGenerationContext";
import { updateTreeViewIcons } from "../util";
import { openTreeViewWithProgress } from "../utilities/progress";
import { Command } from "./Command";

export class EditPathsCommand extends Command {

  private _openApiTreeProvider: OpenApiTreeProvider;
  private _workspaceTreeProvider: WorkspaceTreeProvider;

  public constructor(openApiTreeProvider: OpenApiTreeProvider, workspaceTreeProvider: WorkspaceTreeProvider) {
    super();
    this._openApiTreeProvider = openApiTreeProvider;
    this._workspaceTreeProvider = workspaceTreeProvider;
  }

  public getName(): string {
    return `${extensionId}.editPaths`;
  }

  public async execute({ clientOrPluginKey, clientOrPluginObject }: Partial<WorkspaceGenerationContext>): Promise<void> {
    await this.loadEditPaths(clientOrPluginKey!, clientOrPluginObject!);
    this._openApiTreeProvider.resetInitialState();
    await updateTreeViewIcons(treeViewId, false, true);
    await this._workspaceTreeProvider.refreshView();
  }

  private async loadEditPaths(clientOrPluginKey: string, clientOrPluginObject: ClientOrPluginProperties) {
    await openTreeViewWithProgress(() => this._openApiTreeProvider.loadEditPaths(clientOrPluginKey, clientOrPluginObject));
  }
}
