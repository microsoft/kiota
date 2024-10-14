import { treeViewId } from "../../constants";
import { OpenApiTreeNode, OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { Command } from "../Command";

export class RemoveAllFromSelectedEndpointsCommand extends Command {
  private _openApiTreeProvider: OpenApiTreeProvider;

  constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._openApiTreeProvider = openApiTreeProvider;
  }

  public getName(): string {
    return `${treeViewId}.removeAllFromSelectedEndpoints`;
  }

  public async execute(openApiTreeNode: OpenApiTreeNode): Promise<void> {
    this._openApiTreeProvider.select(openApiTreeNode, false, true);
  }
}
