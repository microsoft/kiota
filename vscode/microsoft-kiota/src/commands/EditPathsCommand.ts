import { treeViewId } from "../constants";
import { ClientOrPluginProperties } from "../kiotaInterop";
import { OpenApiTreeProvider } from "../openApiTreeProvider";
import { updateTreeViewIcons } from "../util";
import { openTreeViewWithProgress } from "../utilities/file";
import { Command } from "./Command";

export class EditPathsCommand extends Command {

  private _openApiTreeProvider: OpenApiTreeProvider;
  private _clientKey: string; 
  private _clientObject: ClientOrPluginProperties; 

  public constructor(openApiTreeProvider: OpenApiTreeProvider, clientKey: string, clientObject: ClientOrPluginProperties) {
    super();
    this._openApiTreeProvider = openApiTreeProvider;
    this._clientKey = clientKey;
    this._clientObject = clientObject;
  }

  async execute(): Promise<void> {
    await this.loadEditPaths();
    this._openApiTreeProvider.resetInitialState();
    await updateTreeViewIcons(treeViewId, false, true);
  }

  async loadEditPaths() {
    await openTreeViewWithProgress(() => this._openApiTreeProvider.loadEditPaths(this._clientKey, this._clientObject));
  }

}