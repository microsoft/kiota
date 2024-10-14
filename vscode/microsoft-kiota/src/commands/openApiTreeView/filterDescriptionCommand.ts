import { treeViewId } from "../../constants";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { filterSteps } from "../../steps";
import { Command } from "../Command";

export class FilterDescriptionCommand extends Command {

  private _openApiTreeProvider: OpenApiTreeProvider;

  constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._openApiTreeProvider = openApiTreeProvider;
  }

  public getName(): string {
    return `${treeViewId}.filterDescription`;
  }

  public async execute(): Promise<void> {
    await filterSteps(this._openApiTreeProvider.filter,
      x => this._openApiTreeProvider.filter = x);
  }

}
