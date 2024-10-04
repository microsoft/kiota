import { extensionId, treeViewId } from "../constants";
import { ClientOrPluginProperties } from "../kiotaInterop";
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

  public async execute({ clientKey, clientObject }: { clientKey: string, clientObject: ClientOrPluginProperties }): Promise<void> {
    await this.loadEditPaths(clientKey, clientObject);
    this._openApiTreeProvider.resetInitialState();
    await updateTreeViewIcons(treeViewId, false, true);
  }

  private async loadEditPaths(clientKey: string, clientObject: ClientOrPluginProperties) {
    await openTreeViewWithProgress(() => this._openApiTreeProvider.loadEditPaths(clientKey, clientObject));
  }
}
