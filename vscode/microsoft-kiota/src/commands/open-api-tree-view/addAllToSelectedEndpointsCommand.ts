import { treeViewId } from "../../constants";
import { OpenApiTreeNode, OpenApiTreeProvider } from "../../openApiTreeProvider";
import { Command } from "../Command";

export class AddAllToSelectedEndpointsCommand extends Command {
  private _openApiTreeProvider: OpenApiTreeProvider;

  constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._openApiTreeProvider = openApiTreeProvider;
  }

  public getName(): string {
    return `${treeViewId}.addAllToSelectedEndpoints`;
  }

  public async execute(openApiTreeNode: OpenApiTreeNode): Promise<void> {
    this._openApiTreeProvider.select(openApiTreeNode, true, true);
  }
}
