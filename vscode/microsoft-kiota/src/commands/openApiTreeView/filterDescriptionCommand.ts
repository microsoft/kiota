import { treeViewId } from "../../constants";
import { filterSteps } from "../../modules/steps/filterSteps";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
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
