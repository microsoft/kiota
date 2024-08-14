import { OpenApiTreeNode, OpenApiTreeProvider } from "../../openApiTreeProvider";
import { Command } from "../Command";

export class AddAllToSelectedEndpointsCommand extends Command {
  private _openApiTreeProvider: OpenApiTreeProvider;
  
  constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();;
    this._openApiTreeProvider = openApiTreeProvider;
  }

  execute(openApiTreeNode: OpenApiTreeNode): void {
    this._openApiTreeProvider.select(openApiTreeNode, true, true);
  }

}