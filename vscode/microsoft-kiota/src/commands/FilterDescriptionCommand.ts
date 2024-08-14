import { treeViewId } from "../constants";
import { OpenApiTreeProvider } from "../providers/openApiTreeProvider";
import { filterSteps } from "../steps";
import { Command } from "./Command";

export class FilterDescriptionCommand extends Command {
  private _openApiTreeProvider: OpenApiTreeProvider;
  
  constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._openApiTreeProvider = openApiTreeProvider;
  }
  
  public toString(): string {
    return `${treeViewId}.filterDescription`;
  }
  
  async execute(): Promise<void> {
    await filterSteps(this._openApiTreeProvider.filter,
      x => this._openApiTreeProvider.filter = x);
  }

}