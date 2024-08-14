import { OpenApiTreeNode, OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { Command } from "../Command";

export class AddToSelectedEndpointsCommand extends Command {

  private _openApiTreeProvider: OpenApiTreeProvider;
  
  constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();;
    this._openApiTreeProvider = openApiTreeProvider;
  }

  execute(openApiTreeNode: OpenApiTreeNode): void {
    this._openApiTreeProvider.select(openApiTreeNode, true, false);
  }

}