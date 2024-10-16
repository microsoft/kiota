import { treeViewId } from "../../constants";
import { OpenApiTreeNode, OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { Command } from "../Command";

export class RemoveFromSelectedEndpointsCommand extends Command {
  private _openApiTreeProvider: OpenApiTreeProvider;

  constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._openApiTreeProvider = openApiTreeProvider;
  }

  public getName(): string {
    return `${treeViewId}.removeFromSelectedEndpoints`;
  }

  public async execute(openApiTreeNode: OpenApiTreeNode): Promise<void> {
    this._openApiTreeProvider.select(openApiTreeNode, false, false);
  }
}
