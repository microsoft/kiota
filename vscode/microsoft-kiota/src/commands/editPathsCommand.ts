import { extensionId, treeViewId } from "../constants";
import { getWorkspaceGenerationContext } from "../handlers/workspaceGenerationContextHandler";
import { OpenApiTreeProvider } from "../openApiTreeProvider";
import { updateTreeViewIcons } from "../util";
import { openTreeViewWithProgress } from "../utilities/progress";
import { Command } from "./Command";

export class EditPathsCommand extends Command {

  private _openApiTreeProvider: OpenApiTreeProvider;

  public constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._openApiTreeProvider = openApiTreeProvider;
  }

  public getName(): string {
    return `${extensionId}.editPaths`;
  }

  public async execute(): Promise<void> {
    await this.loadEditPaths();
    this._openApiTreeProvider.resetInitialState();
    await updateTreeViewIcons(treeViewId, false, true);
  }

  private async loadEditPaths() {
    const { clientOrPluginKey, clientOrPluginObject } = getWorkspaceGenerationContext();
    await openTreeViewWithProgress(() => this._openApiTreeProvider.loadEditPaths(clientOrPluginKey, clientOrPluginObject));
  }
}
