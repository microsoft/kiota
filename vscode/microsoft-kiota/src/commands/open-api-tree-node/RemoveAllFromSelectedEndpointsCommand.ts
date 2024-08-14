import { OpenApiTreeNode, OpenApiTreeProvider } from "../../openApiTreeProvider";
import { Command } from "../Command";

export class RemoveAllFromSelectedEndpointsCommand extends Command {
  private _openApiTreeProvider: OpenApiTreeProvider;
  
  constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();;
    this._openApiTreeProvider = openApiTreeProvider;
  }

  execute(openApiTreeNode: OpenApiTreeNode): void {
    this._openApiTreeProvider.select(openApiTreeNode, false, true);
  }

}